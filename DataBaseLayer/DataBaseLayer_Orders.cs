using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Orders
    {
        Task<IActionResult> CheckOut(string userEmail);
        Task<IActionResult> CreateOrder(string userEmail, IFormCollection form);
        Task<IActionResult> AddOrderItem(IFormCollection form);
        Task<IActionResult> MarkPaymentPaid(int orderId);
        Task<IActionResult> GetOrder(int orderId);
        Task<IActionResult> GetAllOrders();
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Orders
    {

    }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> CheckOut(string userEmail)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                // 1️⃣ Get UserId from AspNetUsers
                Guid userId;
                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email", con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);
                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return new BadRequestObjectResult(new
                        {
                            success = false,
                            message = "User not found"
                        });

                    userId = Guid.Parse(result.ToString());
                }

                // 2️⃣ Get Cart Items
                var cartItems = new List<dynamic>();
                using (var cartCmd = new NpgsqlCommand(@"
                    SELECT course_id, price, discount, quantity
                    FROM cart_items
                    WHERE user_id=@UserId AND is_active=true", con))
                {
                    cartCmd.Parameters.AddWithValue("@UserId", userId);

                    using var reader = await cartCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        cartItems.Add(new
                        {
                            course_id = reader.GetInt32(0),
                            price = reader.GetDecimal(1),
                            discount = reader.GetDecimal(2),
                            quantity = reader.GetInt32(3)
                        });
                    }
                }

                if (!cartItems.Any())
                {
                    return new OkObjectResult(new
                    {
                        success = false,
                        message = "Cart is empty"
                    });
                }

                // 3️⃣ Calculate totals
                decimal subtotal = 0;
                decimal discountTotal = 0;

                foreach (var item in cartItems)
                {
                    subtotal += item.price * item.quantity;
                    discountTotal += item.discount * item.quantity;
                }

                decimal totalAmount = subtotal - discountTotal;

                // 4️⃣ Create Order
                int orderId;
                using (var orderCmd = new NpgsqlCommand(@"
                    INSERT INTO orders
                    (user_id, subtotal, discount_amount, total_amount, payment_status)
                    VALUES
                    (@UserId, @Subtotal, @Discount, @Total, 'pending')
                    RETURNING id", con))
                {
                    orderCmd.Parameters.AddWithValue("@UserId", userId);
                    orderCmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    orderCmd.Parameters.AddWithValue("@Discount", discountTotal);
                    orderCmd.Parameters.AddWithValue("@Total", totalAmount);

                    orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
                }

                // 5️⃣ Insert Order Items
                foreach (var item in cartItems)
                {
                    using var itemCmd = new NpgsqlCommand(@"
                        INSERT INTO order_items
                        (order_id, course_id, price, discount, quantity)
                        VALUES
                        (@OrderId, @CourseId, @Price, @Discount, @Qty)", con);

                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@CourseId", item.course_id);
                    itemCmd.Parameters.AddWithValue("@Price", item.price);
                    itemCmd.Parameters.AddWithValue("@Discount", item.discount);
                    itemCmd.Parameters.AddWithValue("@Qty", item.quantity);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                // 6️⃣ Clear Cart
                using (var clearCmd = new NpgsqlCommand(
                    "DELETE FROM cart_items WHERE user_id=@UserId", con))
                {
                    clearCmd.Parameters.AddWithValue("@UserId", userId);
                    await clearCmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();

                // 7️⃣ Response
                return new OkObjectResult(new
                {
                    success = true,
                    order_id = orderId,
                    subtotal,
                    discount = discountTotal,
                    total_amount = totalAmount
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

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

                cmd.Parameters.AddWithValue("@user_id", userId.Value);
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

        public async Task<IActionResult> MarkPaymentPaid(int orderId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            string query = @"
        UPDATE orders
        SET payment_status='PAID', order_status='CONFIRMED'
        WHERE id=@id
    ";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", orderId);

            await cmd.ExecuteNonQueryAsync();

            return new OkObjectResult(new
            {
                success = true,
                message = "Payment successful & order confirmed"
            });
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

    }
}
