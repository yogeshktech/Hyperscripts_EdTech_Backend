using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Orders
    {
        Task<IActionResult> CreateOrder(string userEmail, IFormCollection form);
        Task<IActionResult> AddOrderItem(IFormCollection form);
        Task<IActionResult> GetOrder(int orderId);
        Task<IActionResult> GetAllOrders();
        Task<IActionResult> UpdateOrderStatus(IFormCollection form);
        Task<IActionResult> UpdatePaymentStatus(IFormCollection form);
        Task<IActionResult> DeleteOrder(int id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Orders
    {

    }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> CreateOrder(string userEmail, IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // Read form values
                string couponCode = form["coupon_code"];   // coupon name from frontend
                string subtotalStr = form["subtotal"];
                string discountAmountStr = form["discount_amount"];
                string totalAmountStr = form["total_amount"];

                if (string.IsNullOrEmpty(userEmail))
                    return new BadRequestObjectResult(new { success = false, message = "User Email required!" });

                // -------------------------------------------------------
                // 1️⃣ Get userId using email
                // -------------------------------------------------------
                Guid? userId = null;

                string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";
                using (var userCmd = new NpgsqlCommand(userQuery, con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);
                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return new BadRequestObjectResult(new { success = false, message = "User not found!" });

                    userId = Guid.Parse(result.ToString());
                }

                // -------------------------------------------------------
                // 2️⃣ Get coupon_id from coupons table using coupon_code
                // -------------------------------------------------------
                int? couponId = null;

                if (!string.IsNullOrEmpty(couponCode))
                {
                    string couponQuery = @"SELECT id FROM coupons WHERE code=@code LIMIT 1";

                    using var couponCmd = new NpgsqlCommand(couponQuery, con);
                    couponCmd.Parameters.AddWithValue("@code", couponCode);

                    var couponResult = await couponCmd.ExecuteScalarAsync();

                    if (couponResult != null)
                        couponId = Convert.ToInt32(couponResult);
                    else
                        return new BadRequestObjectResult(new { success = false, message = "Invalid coupon code!" });
                }

                // Convert values
                decimal subtotal = decimal.Parse(subtotalStr);
                decimal discountAmount = decimal.Parse(discountAmountStr);
                decimal totalAmount = decimal.Parse(totalAmountStr);

                // -------------------------------------------------------
                // 3️⃣ Insert into orders table
                // -------------------------------------------------------
                string insertQuery = @"
            INSERT INTO orders (user_id, coupon_id, subtotal, discount_amount, total_amount)
            VALUES (@user_id, @coupon_id, @subtotal, @discount_amount, @total_amount)
            RETURNING id;
        ";

                using var cmd = new NpgsqlCommand(insertQuery, con);

                cmd.Parameters.AddWithValue("@user_id", userId.ToString());
                cmd.Parameters.Add("@coupon_id", NpgsqlTypes.NpgsqlDbType.Integer)
                              .Value = (object?)couponId ?? DBNull.Value;
                cmd.Parameters.AddWithValue("@subtotal", subtotal);
                cmd.Parameters.AddWithValue("@discount_amount", discountAmount);
                cmd.Parameters.AddWithValue("@total_amount", totalAmount);

                int orderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Order Created Successfully",
                    order_id = orderId,
                    coupon_id = couponId
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> AddOrderItem(IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string orderIdStr = form["order_id"];
                string courseIdStr = form["course_id"];
                string priceStr = form["price"];
                string discountStr = form["discount"];
                string qtyStr = form["quantity"];

                if (!int.TryParse(orderIdStr, out int orderId))
                    return new BadRequestObjectResult(new { success = false, message = "Invalid order_id" });

                int courseId = int.Parse(courseIdStr);
                decimal price = decimal.Parse(priceStr);
                decimal discount = decimal.Parse(discountStr);
                int quantity = int.Parse(qtyStr);

                string query = @"
        INSERT INTO order_items (order_id, course_id, price, discount, quantity)
        VALUES (@order_id, @course_id, @price, @discount, @quantity)
        ";

                using var cmd = new NpgsqlCommand(query, con);

                cmd.Parameters.AddWithValue("@order_id", orderId);
                cmd.Parameters.AddWithValue("@course_id", courseId);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@discount", discount);
                cmd.Parameters.AddWithValue("@quantity", quantity);

                await cmd.ExecuteNonQueryAsync();

                return new OkObjectResult(new { success = true, message = "Item Added Successfully" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string queryOrder = "SELECT * FROM orders WHERE id=@id";
                string queryItems = "SELECT * FROM order_items WHERE order_id=@id";

                var order = new Dictionary<string, object>();
                var items = new List<Dictionary<string, object>>();

                using (var cmd = new NpgsqlCommand(queryOrder, con))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        order["id"] = reader["id"];
                        order["user_id"] = reader["user_id"];
                        order["subtotal"] = reader["subtotal"];
                        order["discount_amount"] = reader["discount_amount"];
                        order["total_amount"] = reader["total_amount"];
                        order["payment_status"] = reader["payment_status"];
                        order["order_status"] = reader["order_status"];
                    }
                    else
                    {
                        return new NotFoundObjectResult(new { success = false, message = "Order not found" });
                    }
                }

                using (var cmd = new NpgsqlCommand(queryItems, con))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        items.Add(new Dictionary<string, object>()
                {
                    { "course_id", reader["course_id"] },
                    { "price", reader["price"] },
                    { "discount", reader["discount"] },
                    { "quantity", reader["quantity"] }
                });
                    }
                }

                return new OkObjectResult(new { success = true, order, items });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "SELECT * FROM orders ORDER BY id DESC";

                var list = new List<Dictionary<string, object>>();

                using var cmd = new NpgsqlCommand(query, con);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new Dictionary<string, object>
            {
                { "id", reader["id"] },
                { "user_id", reader["user_id"] },
                { "subtotal", reader["subtotal"] },
                { "discount_amount", reader["discount_amount"] },
                { "total_amount", reader["total_amount"] },
                { "payment_status", reader["payment_status"] },
                { "order_status", reader["order_status"] }
            });
                }

                return new OkObjectResult(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateOrderStatus(IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                int orderId = int.Parse(form["order_id"]);
                string status = form["order_status"];

                string query = @"UPDATE orders SET order_status=@status, updated_at=NOW() WHERE id=@id";

                using var cmd = new NpgsqlCommand(query, con);

                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.Parameters.AddWithValue("@status", status);

                await cmd.ExecuteNonQueryAsync();

                return new OkObjectResult(new { success = true, message = "Order status updated!" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdatePaymentStatus(IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                int orderId = int.Parse(form["order_id"]);
                string status = form["payment_status"];

                string query = @"UPDATE orders SET payment_status=@status, updated_at=NOW() WHERE id=@id";

                using var cmd = new NpgsqlCommand(query, con);

                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.Parameters.AddWithValue("@status", status);

                await cmd.ExecuteNonQueryAsync();

                return new OkObjectResult(new { success = true, message = "Payment status updated!" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "DELETE FROM orders WHERE id=@id";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return new OkObjectResult(new { success = true, message = "Order deleted successfully!" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }

    }
}
