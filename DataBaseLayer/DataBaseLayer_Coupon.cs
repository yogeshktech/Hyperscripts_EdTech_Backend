using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Coupon
    {
        Task<IActionResult> AddCoupon(IFormCollection form);
        Task<IActionResult> GetAllCoupons();
        Task<IActionResult> GetCouponById(int id);
        Task<IActionResult> UpdateCoupon(int id, IFormCollection form);
        Task<IActionResult> DeleteCoupon(int id);
        Task<IActionResult> ToggleCoupon(int id);
    }
    public partial interface IDataBaseLayer : IDataBaseLayer_Coupon { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> AddCoupon(IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // 1️⃣ Read form values
                string code = form["couponName"];
                string discountType = form["discount_type"];
                string discountValueStr = form["discount_value"];
                string minOrderValueStr = form["min_order_value"];
                string maxDiscountStr = form["max_discount"];
                string courseIdStr = form["course_id"];
                string usageLimitUserStr = form["usage_limit_per_user"];
                string totalUsageLimitStr = form["total_usage_limit"];
                string startDateStr = form["start_date"];
                string endDateStr = form["end_date"];
                string isActiveStr = form["is_active"];

                // 2️⃣ VALIDATION
                if (string.IsNullOrEmpty(code))
                    return BadRequest(new { success = false, message = "Coupon code is required!" });

                if (!decimal.TryParse(discountValueStr, out decimal discountValue))
                    return BadRequest(new { success = false, message = "Invalid discount value" });

                decimal.TryParse(minOrderValueStr, out decimal minOrderValue);

                // Handle nullable decimal (max_discount)
                decimal? maxDiscount = null;
                if (!string.IsNullOrEmpty(maxDiscountStr))
                {
                    if (decimal.TryParse(maxDiscountStr, out decimal parsedValue))
                        maxDiscount = parsedValue;
                }

                // Handle nullable int (course_id)
                int? courseId = null;
                if (!string.IsNullOrEmpty(courseIdStr))
                {
                    if (int.TryParse(courseIdStr, out int parsedCourseId))
                        courseId = parsedCourseId;
                }

                int.TryParse(usageLimitUserStr, out int usageLimitUser);
                int.TryParse(totalUsageLimitStr, out int totalUsageLimit);

                DateTime startDate = DateTime.Parse(startDateStr);
                DateTime endDate = DateTime.Parse(endDateStr);

                bool isActive = isActiveStr == "true";

                // 3️⃣ Insert Query
                string query = @"
            INSERT INTO coupons 
            (code, discount_type, discount_value, min_order_value, max_discount, course_id, 
             usage_limit_per_user, total_usage_limit, start_date, end_date, is_active)
            VALUES
            (@code, @discount_type, @discount_value, @min_order_value, @max_discount, @course_id,
             @usage_limit_per_user, @total_usage_limit, @start_date, @end_date, @is_active)
        ";

                using var cmd = new NpgsqlCommand(query, con);

                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@discount_type", discountType);
                cmd.Parameters.AddWithValue("@discount_value", discountValue);
                cmd.Parameters.AddWithValue("@min_order_value", minOrderValue);

                // FIXED nullable decimal
                cmd.Parameters.Add("@max_discount", NpgsqlTypes.NpgsqlDbType.Numeric)
                              .Value = (object?)maxDiscount ?? DBNull.Value;

                // FIXED nullable int
                cmd.Parameters.Add("@course_id", NpgsqlTypes.NpgsqlDbType.Integer)
                              .Value = (object?)courseId ?? DBNull.Value;

                cmd.Parameters.AddWithValue("@usage_limit_per_user",
                    usageLimitUser == 0 ? 1 : usageLimitUser);

                cmd.Parameters.AddWithValue("@total_usage_limit",
                    totalUsageLimit == 0 ? 1000 : totalUsageLimit);

                cmd.Parameters.AddWithValue("@start_date", startDate);
                cmd.Parameters.AddWithValue("@end_date", endDate);
                cmd.Parameters.AddWithValue("@is_active", isActive);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true, message = "Coupon added successfully!" });
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "23505")
                    return BadRequest(new { success = false, message = "Coupon code already exists!" });

                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAllCoupons()
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "SELECT * FROM coupons ORDER BY id DESC";

                using var cmd = new NpgsqlCommand(query, con);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["id"],
                        code = reader["code"],
                        discount_type = reader["discount_type"],
                        discount_value = reader["discount_value"],
                        min_order_value = reader["min_order_value"],
                        max_discount = reader["max_discount"] == DBNull.Value ? null : reader["max_discount"],
                        course_id = reader["course_id"] == DBNull.Value ? null : reader["course_id"],
                        usage_limit_per_user = reader["usage_limit_per_user"],
                        total_usage_limit = reader["total_usage_limit"],
                        start_date = reader["start_date"],
                        end_date = reader["end_date"],
                        is_active = reader["is_active"],
                        created_at = reader["created_at"],
                        updated_at = reader["updated_at"]
                    });
                }

                return Ok(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetCouponById(int id)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "SELECT * FROM coupons WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return NotFound(new { success = false, message = "Coupon not found" });

                await reader.ReadAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = reader["id"],
                        code = reader["code"],
                        discount_type = reader["discount_type"],
                        discount_value = reader["discount_value"],
                        min_order_value = reader["min_order_value"],
                        max_discount = reader["max_discount"] == DBNull.Value ? null : reader["max_discount"],
                        course_id = reader["course_id"] == DBNull.Value ? null : reader["course_id"],
                        usage_limit_per_user = reader["usage_limit_per_user"],
                        total_usage_limit = reader["total_usage_limit"],
                        start_date = reader["start_date"],
                        end_date = reader["end_date"],
                        is_active = reader["is_active"]
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateCoupon(int id, IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // ------------------------------
                // 1️⃣ Fetch existing coupon
                // ------------------------------
                string selectQuery = "SELECT * FROM coupons WHERE id = @id";

                dynamic? existingCoupon = null;

                using (var selectCmd = new NpgsqlCommand(selectQuery, con))
                {
                    selectCmd.Parameters.AddWithValue("@id", id);

                    using var reader = await selectCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        existingCoupon = new
                        {
                            id = reader["id"],
                            code = reader["code"],
                            discount_type = reader["discount_type"],
                            discount_value = reader["discount_value"],
                            min_order_value = reader["min_order_value"],
                            max_discount = reader["max_discount"],
                            course_id = reader["course_id"],
                            usage_limit_per_user = reader["usage_limit_per_user"],
                            total_usage_limit = reader["total_usage_limit"],
                            start_date = reader["start_date"],
                            end_date = reader["end_date"],
                            is_active = reader["is_active"]
                        };
                    }
                }

                if (existingCoupon == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Coupon not found"
                    });
                }

                // ------------------------------
                // 2️⃣ Read & parse new form values
                // ------------------------------
                string code = form["couponName"];
                string discountType = form["discount_type"];
                string discountValueStr = form["discount_value"];
                string minOrderValueStr = form["min_order_value"];
                string maxDiscountStr = form["max_discount"];
                string courseIdStr = form["course_id"];
                string usageLimitUserStr = form["usage_limit_per_user"];
                string totalUsageLimitStr = form["total_usage_limit"];
                string startDateStr = form["start_date"];
                string endDateStr = form["end_date"];
                string isActiveStr = form["is_active"];

                // 3️⃣ Check if same CODE already exists for another coupon
                string checkQuery = "SELECT COUNT(*) FROM coupons WHERE code = @code AND id != @id";

                using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@code", code);
                    checkCmd.Parameters.AddWithValue("@id", id);

                    long count = (long)await checkCmd.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Coupon code already exists! Use a different code."
                        });
                    }
                }

                // Parse numeric values
                if (!decimal.TryParse(discountValueStr, out decimal discountValue))
                    return BadRequest(new { success = false, message = "Invalid discount value" });

                decimal.TryParse(minOrderValueStr, out decimal minOrderValue);

                decimal? maxDiscount = null;
                if (!string.IsNullOrEmpty(maxDiscountStr) &&
                    decimal.TryParse(maxDiscountStr, out var tmp1)) maxDiscount = tmp1;

                int? courseId = null;
                if (!string.IsNullOrEmpty(courseIdStr) &&
                    int.TryParse(courseIdStr, out var tmp2)) courseId = tmp2;

                int.TryParse(usageLimitUserStr, out int usageLimitUser);
                int.TryParse(totalUsageLimitStr, out int totalUsageLimit);

                DateTime startDate = DateTime.Parse(startDateStr);
                DateTime endDate = DateTime.Parse(endDateStr);

                bool isActive = isActiveStr == "true";

                // ------------------------------
                // 4️⃣ Update coupon
                // ------------------------------
                string updateQuery = @"
            UPDATE coupons SET 
                code = @code,
                discount_type = @discount_type,
                discount_value = @discount_value,
                min_order_value = @min_order_value,
                max_discount = @max_discount,
                course_id = @course_id,
                usage_limit_per_user = @usage_limit_per_user,
                total_usage_limit = @total_usage_limit,
                start_date = @start_date,
                end_date = @end_date,
                is_active = @is_active,
                updated_at = NOW()
            WHERE id = @id
        ";

                using var cmd = new NpgsqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@discount_type", discountType);
                cmd.Parameters.AddWithValue("@discount_value", discountValue);
                cmd.Parameters.AddWithValue("@min_order_value", minOrderValue);

                cmd.Parameters.Add("@max_discount", NpgsqlTypes.NpgsqlDbType.Numeric)
                              .Value = (object?)maxDiscount ?? DBNull.Value;

                cmd.Parameters.Add("@course_id", NpgsqlTypes.NpgsqlDbType.Integer)
                              .Value = (object?)courseId ?? DBNull.Value;

                cmd.Parameters.AddWithValue("@usage_limit_per_user", usageLimitUser);
                cmd.Parameters.AddWithValue("@total_usage_limit", totalUsageLimit);
                cmd.Parameters.AddWithValue("@start_date", startDate);
                cmd.Parameters.AddWithValue("@end_date", endDate);
                cmd.Parameters.AddWithValue("@is_active", isActive);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    success = true,
                    message = "Coupon updated successfully!",
                    old_coupon = existingCoupon
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteCoupon(int id)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "DELETE FROM coupons WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return NotFound(new { success = false, message = "Coupon not found!" });

                return Ok(new { success = true, message = "Coupon deleted successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ToggleCoupon(int id)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = @"
            UPDATE coupons
            SET is_active = NOT is_active,
                updated_at = NOW()
            WHERE id = @id
            RETURNING is_active;
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return NotFound(new { success = false, message = "Coupon not found!" });

                return Ok(new { success = true, new_status = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

    }
}
