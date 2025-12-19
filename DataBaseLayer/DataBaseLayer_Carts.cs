using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Carts
    {
        Task<IActionResult> AddToCart(int courseId, string userEmail, string clientIp);
        Task<IActionResult> GetToCart(string userEmail, string ip);
        Task<IActionResult> DeleteCart(int cartId);
        Task<IActionResult> ClearCart(string userEmail, string clientIp);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Carts { }
    public partial class DataBaseLayer
    {

        public async Task<IActionResult> AddToCart(int courseId, string userEmail, string ip)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                Guid? userId = null;

                // -------------------------------------------------------
                // 1️⃣ Logged-in user → get userId
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
                        return BadRequest(new { success = false, message = "Course not found!" });

                    mrp = rd.GetDecimal(0);
                    sale = rd.GetDecimal(1);
                }
                decimal dis = mrp - sale;

                // -------------------------------------------------------
                // 3️⃣ Merge guest cart → logged-in cart
                // -------------------------------------------------------
                if (userId.HasValue)
                {
                    string mergeQuery = @"
                UPDATE cart_items 
                SET user_id = @uid, ip_address = NULL
                WHERE ip_address = @ip AND user_id IS NULL AND is_active = TRUE";

                    using var mergeCmd = new NpgsqlCommand(mergeQuery, con);
                    mergeCmd.Parameters.AddWithValue("@uid", userId.Value);
                    mergeCmd.Parameters.AddWithValue("@ip", ip ?? "");

                    await mergeCmd.ExecuteNonQueryAsync();
                }

                // -------------------------------------------------------
                // 4️⃣ CHECK IF COURSE ALREADY IN CART
                // -------------------------------------------------------
                string checkQuery = @"
            SELECT id 
            FROM cart_items 
            WHERE course_id = @pid
            AND is_active = TRUE
            AND (
                    (user_id = @uid)
                OR  (user_id IS NULL AND ip_address = @ip)
                )
            LIMIT 1";

                int? existingId = null;

                using (var check = new NpgsqlCommand(checkQuery, con))
                {
                    check.Parameters.AddWithValue("@pid", courseId);
                    check.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
                    check.Parameters.AddWithValue("@ip", (object?)ip ?? DBNull.Value);

                    var rd = await check.ExecuteScalarAsync();
                    if (rd != null)
                        existingId = Convert.ToInt32(rd);
                }

                // ⭐ If exists → do NOT update quantity (always 1)
                if (existingId.HasValue)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Course already in cart"
                    });
                }

                // -------------------------------------------------------
                // 5️⃣ INSERT NEW CART RECORD
                // -------------------------------------------------------
                string insert = @"
            INSERT INTO cart_items 
            (user_id, ip_address, course_id, quantity, price, discount, is_active, saling_price)
            VALUES 
            (@uid, @ip, @pid, 1, @price, @discount, TRUE,@saling_price)";

                using var ins = new NpgsqlCommand(insert, con);

                // ✔ If userId exists → save userId & NULL IP
                if (userId.HasValue)
                {
                    ins.Parameters.AddWithValue("@uid", userId.Value);
                    ins.Parameters.AddWithValue("@ip", DBNull.Value);
                }
                else
                {
                    // ✔ Guest (not logged-in) → save IP & NULL userId
                    ins.Parameters.AddWithValue("@uid", DBNull.Value);
                    ins.Parameters.AddWithValue("@ip", ip ?? "");
                }

                ins.Parameters.AddWithValue("@pid", courseId);
                ins.Parameters.AddWithValue("@price", mrp);
                ins.Parameters.AddWithValue("@discount", dis);
                ins.Parameters.AddWithValue("@saling_price", sale);

                await ins.ExecuteNonQueryAsync();

                return Ok(new { success = true, message = "Course added to cart" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetToCart(string userEmail, string ip)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                Guid? userId = null;

                // ✔ Get userId if logged in
                if (!string.IsNullOrEmpty(userEmail))
                {
                    string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";
                    using var cmd = new NpgsqlCommand(userQuery, con);
                    cmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                        userId = Guid.Parse(result.ToString());
                }

                // ✔ Query cart + course details
                string query = @"
            SELECT ci.id AS cart_id,
                   ci.course_id,
                   ci.quantity,
                   ci.price,
                   ci.discount,
                   c.course_name,
                   c.course_discription,
                   c.category_id,
                   c.course_image,
                   c.mrp_price,
                   c.saling_price
            FROM cart_items ci
            JOIN courses c ON c.id = ci.course_id
            WHERE ci.is_active = TRUE
            AND (
                (ci.user_id = @uid)
                OR
                (ci.user_id IS NULL AND ci.ip_address = @ip)
            )
            ORDER BY ci.id DESC";

                using var cartCmd = new NpgsqlCommand(query, con);
                cartCmd.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
                cartCmd.Parameters.AddWithValue("@ip", (object?)ip ?? DBNull.Value);

                using var reader = await cartCmd.ExecuteReaderAsync();

                var cartItems = new List<object>();

                while (await reader.ReadAsync())
                {
                    cartItems.Add(new
                    {
                        cartId = reader.GetInt32(0),
                        courseId = reader.GetInt32(1),
                        quantity = reader.GetInt32(2),
                        price = reader.GetDecimal(3),
                        discount = reader.GetDecimal(4),
                        courseName = reader.GetString(5),
                        description = reader.GetString(6),
                        categoryId = reader.GetInt32(7),
                        imageUrl = reader.GetString(8),
                        mrpPrice = reader.GetDecimal(9),
                        salePrice = reader.GetDecimal(10)
                    });
                }

                return Ok(new { success = true, data = cartItems });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteCart(int cartId)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // check cart 

                string getItem = @"select discount from cart_items where id = @cartId";
                var cartItem = 0;
                using (var selectCmd = new NpgsqlCommand(getItem, con))
                {
                    selectCmd.Parameters.AddWithValue("@cartId", cartId);
                    var result = await selectCmd.ExecuteScalarAsync();

                    if(result == null )
                    {
                        return BadRequest(new { success = false, message = "cart not found" });
                    }

                    string deleteItem = @"delete from cart_items where id = @cartId";
                    using (var deleteCmd = new NpgsqlCommand(deleteItem, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@cartId", cartId);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }


                    return Ok(new {success = true, message = "Cart Item delete successfully", id = cartId });
                }


                //return Ok(result)
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public async Task<IActionResult> ClearCart(string userEmail, string clientIp)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();
                using var tran = await con.BeginTransactionAsync();

                Guid? userId = null;

                // -------------------------------------------------------
                // 1️⃣ Logged-in user → get userId
                // -------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    using var userCmd = new NpgsqlCommand(
                        @"SELECT ""Id"" 
                  FROM ""AspNetUsers"" 
                  WHERE ""Email"" = @Email 
                  LIMIT 1", con, tran);

                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result != null)
                        userId = Guid.Parse(result.ToString());
                }

                // -------------------------------------------------------
                // 2️⃣ Clear cart
                // -------------------------------------------------------
                int rowsAffected;

                if (userId.HasValue)
                {
                    // Logged-in user cart
                    using var clearCmd = new NpgsqlCommand(
                        @"DELETE FROM cart_items WHERE user_id = @UserId", con, tran);

                    clearCmd.Parameters.AddWithValue("@UserId", userId.Value);
                    rowsAffected = await clearCmd.ExecuteNonQueryAsync();
                }
                else if (!string.IsNullOrWhiteSpace(clientIp))
                {
                    // Guest cart
                    using var clearCmd = new NpgsqlCommand(
                        @"DELETE FROM cart_items WHERE client_ip = @ClientIp", con, tran);

                    clearCmd.Parameters.AddWithValue("@ClientIp", clientIp);
                    rowsAffected = await clearCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User email or client IP is required"
                    });
                }

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cart cleared successfully",
                    removedItems = rowsAffected
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
