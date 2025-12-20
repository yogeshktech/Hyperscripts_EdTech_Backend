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
        Task<IActionResult> ApplyCoupon(IFormCollection form);
    }
    public partial interface IDataBaseLayer : IDataBaseLayer_Coupon { }

    public partial class DataBaseLayer
    {
        //public async Task<IActionResult> AddCoupon(IFormCollection form)
        //{
        //    try
        //    {
        //        using var con = new NpgsqlConnection(DbConnection);
        //        await con.OpenAsync();

        //        // -------------------------------
        //        // 1️⃣ Read Form Values
        //        // -------------------------------
        //        string code = form["couponName"];
        //        string discountType = form["discount_type"];
        //        string discountValueStr = form["discount_value"];
        //        string minOrderValueStr = form["min_order_value"];
        //        string maxDiscountStr = form["max_discount"];
        //        string courseIdStr = form["course_id"];
        //        string usageLimitUserStr = form["usage_limit_per_user"];
        //        string totalUsageLimitStr = form["total_usage_limit"];
        //        string startDateStr = form["start_date"];
        //        string endDateStr = form["end_date"];
        //        string isActiveStr = form["is_active"];

        //        // -------------------------------
        //        // 2️⃣ Validations
        //        // -------------------------------
        //        if (string.IsNullOrWhiteSpace(code))
        //            return BadRequest(new { success = false, message = "Coupon code is required" });

        //        if (!decimal.TryParse(discountValueStr, out decimal discountValue))
        //            return BadRequest(new { success = false, message = "Invalid discount value" });

        //        if (!decimal.TryParse(minOrderValueStr, out decimal minOrderValue))
        //            minOrderValue = 0;

        //        decimal? maxDiscount = null;
        //        if (!string.IsNullOrWhiteSpace(maxDiscountStr) &&
        //            decimal.TryParse(maxDiscountStr, out decimal parsedMax))
        //        {
        //            maxDiscount = parsedMax;
        //        }

        //        int? courseId = null;
        //        if (!string.IsNullOrWhiteSpace(courseIdStr))
        //        {
        //            if (!int.TryParse(courseIdStr, out int parsedCourseId))
        //                return BadRequest(new { success = false, message = "Invalid course_id" });

        //            courseId = parsedCourseId;
        //        }

        //        int usageLimitUser = int.TryParse(usageLimitUserStr, out int ul) ? ul : 1;
        //        int totalUsageLimit = int.TryParse(totalUsageLimitStr, out int tl) ? tl : 1000;

        //        if (!DateTime.TryParse(startDateStr, out DateTime startDate))
        //            return BadRequest(new { success = false, message = "Invalid start date" });

        //        if (!DateTime.TryParse(endDateStr, out DateTime endDate))
        //            return BadRequest(new { success = false, message = "Invalid end date" });

        //        if (endDate < startDate)
        //            return BadRequest(new { success = false, message = "End date must be after start date" });

        //        bool isActive = isActiveStr == "true";

        //        // -------------------------------
        //        // 3️⃣ Validate course_id (FK safety)
        //        // -------------------------------
        //        if (courseId.HasValue)
        //        {
        //            string checkCourseQuery = "SELECT 1 FROM courses WHERE id = @id";
        //            using var checkCmd = new NpgsqlCommand(checkCourseQuery, con);
        //            checkCmd.Parameters.AddWithValue("@id", courseId.Value);

        //            var exists = await checkCmd.ExecuteScalarAsync();
        //            if (exists == null)
        //            {
        //                return BadRequest(new
        //                {
        //                    success = false,
        //                    message = "Invalid course_id. Course does not exist."
        //                });
        //            }
        //        }

        //        // -------------------------------
        //        // 4️⃣ Insert Coupon
        //        // -------------------------------
        //        string insertQuery = @"
        //    INSERT INTO coupons
        //    (code, discount_type, discount_value, min_order_value, max_discount, course_id,
        //     usage_limit_per_user, total_usage_limit, start_date, end_date, is_active)
        //    VALUES
        //    (@code, @discount_type, @discount_value, @min_order_value, @max_discount, @course_id,
        //     @usage_limit_per_user, @total_usage_limit, @start_date, @end_date, @is_active)";

        //        using var cmd = new NpgsqlCommand(insertQuery, con);

        //        cmd.Parameters.AddWithValue("@code", code);
        //        cmd.Parameters.AddWithValue("@discount_type", discountType);
        //        cmd.Parameters.AddWithValue("@discount_value", discountValue);
        //        cmd.Parameters.AddWithValue("@min_order_value", minOrderValue);

        //        cmd.Parameters.Add("@max_discount", NpgsqlTypes.NpgsqlDbType.Numeric)
        //            .Value = (object?)maxDiscount ?? DBNull.Value;

        //        cmd.Parameters.Add("@course_id", NpgsqlTypes.NpgsqlDbType.Integer)
        //            .Value = (object?)courseId ?? DBNull.Value;

        //        cmd.Parameters.AddWithValue("@usage_limit_per_user", usageLimitUser);
        //        cmd.Parameters.AddWithValue("@total_usage_limit", totalUsageLimit);
        //        cmd.Parameters.AddWithValue("@start_date", startDate);
        //        cmd.Parameters.AddWithValue("@end_date", endDate);
        //        cmd.Parameters.AddWithValue("@is_active", isActive);

        //        await cmd.ExecuteNonQueryAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Coupon added successfully"
        //        });
        //    }
        //    catch (PostgresException ex)
        //    {
        //        if (ex.SqlState == "23505")
        //        {
        //            return BadRequest(new
        //            {
        //                success = false,
        //                message = "Coupon code already exists"
        //            });
        //        }

        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        public async Task<IActionResult> AddCoupon(IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // -------------------------------------------------
                // 1️⃣ Read Form Values
                // -------------------------------------------------
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

                // -------------------------------------------------
                // 2️⃣ Basic Validations
                // -------------------------------------------------
                if (string.IsNullOrWhiteSpace(code))
                    return BadRequest(new { success = false, message = "Coupon code is required" });

                if (string.IsNullOrWhiteSpace(discountType))
                    return BadRequest(new { success = false, message = "discount_type is required" });

                discountType = discountType.Trim().ToUpper();

                if (discountType != "FLAT" && discountType != "PERCENT")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid discount_type. Allowed values: FLAT, PERCENT"
                    });
                }

                if (!decimal.TryParse(discountValueStr, out decimal discountValue))
                    return BadRequest(new { success = false, message = "Invalid discount_value" });

                if (!decimal.TryParse(minOrderValueStr, out decimal minOrderValue))
                    minOrderValue = 0;

                decimal? maxDiscount = null;
                if (!string.IsNullOrWhiteSpace(maxDiscountStr) &&
                    decimal.TryParse(maxDiscountStr, out decimal parsedMax))
                {
                    maxDiscount = parsedMax;
                }

                int? courseId = null;
                if (!string.IsNullOrWhiteSpace(courseIdStr))
                {
                    if (!int.TryParse(courseIdStr, out int parsedCourseId))
                        return BadRequest(new { success = false, message = "Invalid course_id" });

                    courseId = parsedCourseId;
                }

                int usageLimitUser = int.TryParse(usageLimitUserStr, out int ul) ? ul : 1;
                int totalUsageLimit = int.TryParse(totalUsageLimitStr, out int tl) ? tl : 1000;

                if (!DateTime.TryParse(startDateStr, out DateTime startDate))
                    return BadRequest(new { success = false, message = "Invalid start_date" });

                if (!DateTime.TryParse(endDateStr, out DateTime endDate))
                    return BadRequest(new { success = false, message = "Invalid end_date" });

                if (endDate < startDate)
                    return BadRequest(new { success = false, message = "End date must be after start date" });

                bool isActive = isActiveStr == "true";

                // -------------------------------------------------
                // 3️⃣ Discount-Type Specific Validation (IMPORTANT)
                // -------------------------------------------------

                // ✅ PERCENTAGE COUPON
                if (discountType == "PERCENT")
                {
                    // Percentage must be 1–100 (NOT 0.10 / 10%)
                    if (discountValue <= 0 || discountValue > 100)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Percentage discount must be between 1 and 100"
                        });
                    }

                    if (maxDiscount == null || maxDiscount <= 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "max_discount is required for percentage coupons"
                        });
                    }
                }

                // ✅ FLAT COUPON
                if (discountType == "FLAT")
                {
                    if (discountValue <= 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "FLAT discount must be greater than 0"
                        });
                    }

                    // Flat discount does not need max_discount
                    maxDiscount = null;
                }

                // -------------------------------------------------
                // 4️⃣ Validate course_id (FK Safety)
                // -------------------------------------------------
                if (courseId.HasValue)
                {
                    string checkCourseQuery = "SELECT 1 FROM courses WHERE id = @id";
                    using var checkCmd = new NpgsqlCommand(checkCourseQuery, con);
                    checkCmd.Parameters.AddWithValue("@id", courseId.Value);

                    if (await checkCmd.ExecuteScalarAsync() == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid course_id. Course does not exist."
                        });
                    }
                }

                // -------------------------------------------------
                // 5️⃣ Insert Coupon
                // -------------------------------------------------
                string insertQuery = @"
            INSERT INTO coupons
            (code, discount_type, discount_value, min_order_value, max_discount, course_id,
             usage_limit_per_user, total_usage_limit, start_date, end_date, is_active)
            VALUES
            (@code, @discount_type, @discount_value, @min_order_value, @max_discount, @course_id,
             @usage_limit_per_user, @total_usage_limit, @start_date, @end_date, @is_active)";

                using var cmd = new NpgsqlCommand(insertQuery, con);

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
                    message = "Coupon added successfully"
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Coupon code already exists"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
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

        public async Task<IActionResult> ApplyCoupon(IFormCollection form)
{
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string couponName = form["couponName"];
                decimal subtotal = decimal.Parse(form["subtotal"]);

                // 1️⃣ Validate coupon
                string query = @"
                    SELECT id, discount_type, discount_value, min_order_value, max_discount
                    FROM coupons
                    WHERE code = @code
                      AND is_active = TRUE
                      AND start_date <= NOW()
                      AND end_date >= NOW()";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@code", couponName);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.Read())
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Invalid or expired coupon"
                    });
                }

                int couponId = Convert.ToInt32(reader["id"]);
                string discountType = reader["discount_type"].ToString();
                decimal discountValue = Convert.ToDecimal(reader["discount_value"]);
                decimal minOrder = Convert.ToDecimal(reader["min_order_value"]);
                decimal maxDiscount = Convert.ToDecimal(reader["max_discount"]);

                // 2️⃣ Minimum order check
                if (subtotal < minOrder)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Minimum order value should be ₹{minOrder}"
                    });
                }

                // 3️⃣ Calculate discount
                decimal discount = 0;

                if (discountType == "PERCENT")
                {
                    discount = subtotal * (discountValue / 100);
                    if (discount > maxDiscount)
                        discount = maxDiscount;
                }
                else if (discountType == "FLAT")
                {
                    discount = discountValue;
                }

                decimal total = subtotal - discount;

                // ✅ DO NOT SAVE ANYTHING HERE
                // Coupon is saved ONLY when order is placed

                return Ok(new
                {
                    success = true,
                    message = "Coupon apply successfully.",
                    coupon_id = couponId,   // frontend stores this temporarily
                    discount,
                    total
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
}

    }
}
