using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Carts
    {
        Task<IActionResult> AddToCart(int courseId, string userEmail, string clientIp);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Carts { }
    public partial class DataBaseLayer
    {
        public interface IDataBaseLayer_Carts
        {
            Task<IActionResult> AddToCart(int courseId, string userEmail, string ip);
        }
            public async Task<IActionResult> AddToCart(int courseId, string userEmail, string ip)
            {
                try
                {
                    using var con = new NpgsqlConnection(DbConnection);
                    await con.OpenAsync();

                    Guid? userId = null;

                    // -------------------------------------------------------
                    // 1️⃣ If email present → get userId (logged-in user)
                    // -------------------------------------------------------
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

                        using var cmd = new NpgsqlCommand(userQuery, con);
                        cmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null)
                            userId = Guid.Parse(result.ToString());
                    }

                    // -------------------------------------------------------
                    // 2️⃣ Get course prices
                    // -------------------------------------------------------
                    string courseQuery = @"SELECT mrp_price, saling_price FROM courses WHERE id=@id";
                    decimal mrp = 0, sale = 0;

                    using (var getCourse = new NpgsqlCommand(courseQuery, con))
                    {
                        getCourse.Parameters.AddWithValue("@id", courseId);
                        using var rd = await getCourse.ExecuteReaderAsync();

                        if (!await rd.ReadAsync())
                            return BadRequest(new { success = false, message = "Course not found" });

                        mrp = rd.GetDecimal(0);
                        sale = rd.GetDecimal(1);
                    }

                    // -------------------------------------------------------
                    // 3️⃣ Merge guest cart → logged-in
                    // -------------------------------------------------------
                    if (userId.HasValue && !string.IsNullOrEmpty(ip))
                    {
                        string mergeQuery = @"
                    UPDATE cart_items 
                    SET user_id = @uid, ip_address = NULL
                    WHERE ip_address = @ip AND user_id IS NULL AND is_active = TRUE";

                        using var mergeCmd = new NpgsqlCommand(mergeQuery, con);
                        mergeCmd.Parameters.AddWithValue("@uid", userId.Value);
                        mergeCmd.Parameters.AddWithValue("@ip", ip);

                        await mergeCmd.ExecuteNonQueryAsync();
                    }

                    // -------------------------------------------------------
                    // 4️⃣ Check if item exists
                    // -------------------------------------------------------
                    string checkQuery = @"
                SELECT id, quantity FROM cart_items 
                WHERE course_id=@pid 
                AND is_active = TRUE
                AND (
                        (user_id=@uid) 
                        OR 
                        (user_id IS NULL AND ip_address=@ip)
                    )
                LIMIT 1";

                    int? existingId = null;
                    int quantity = 1;
                    int oldQty = 0;

                    using (var check = new NpgsqlCommand(checkQuery, con))
                    {
                        check.Parameters.AddWithValue("@pid", courseId);
                        check.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
                        check.Parameters.AddWithValue("@ip", (object?)ip ?? DBNull.Value);

                        using var rd = await check.ExecuteReaderAsync();
                        if (await rd.ReadAsync())
                        {
                            existingId = rd.GetInt32(0);
                            oldQty = rd.GetInt32(1);
                        }
                    }

                    // -------------------------------------------------------
                    // 5️⃣ Update existing
                    // -------------------------------------------------------
                    if (existingId.HasValue)
                    {
                        string upd = @"UPDATE cart_items SET quantity=@qty WHERE id=@id";

                        using var updCmd = new NpgsqlCommand(upd, con);
                        updCmd.Parameters.AddWithValue("@qty", oldQty + 1);
                        updCmd.Parameters.AddWithValue("@id", existingId.Value);
                        await updCmd.ExecuteNonQueryAsync();

                        return Ok(new { success = true, message = "Quantity updated" });
                    }

                    // -------------------------------------------------------
                    // 6️⃣ Insert new cart item
                    // -------------------------------------------------------
                    string insert = @"
                INSERT INTO cart_items (user_id, ip_address, course_id, quantity, price, discount, is_active) 
                VALUES (@uid, @ip, @pid, @qty, @price, @discount, TRUE)";

                    using var ins = new NpgsqlCommand(insert, con);
                    ins.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@ip", (object?)ip ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@pid", courseId);
                    ins.Parameters.AddWithValue("@qty", quantity);
                    ins.Parameters.AddWithValue("@price", mrp);
                    ins.Parameters.AddWithValue("@discount", sale);

                    await ins.ExecuteNonQueryAsync();

                    return Ok(new { success = true, message = "Course added to cart" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }
        


    }
}
