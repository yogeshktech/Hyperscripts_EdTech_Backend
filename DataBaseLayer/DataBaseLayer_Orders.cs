using CareerCracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Npgsql;
using Razorpay.Api;
using System.Data;
using System.Security.Cryptography;
using System.Text;


namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Orders
    {
        Task<IActionResult> CheckOut(string userEmail, IFormCollection form);
        Task<IActionResult> BuyNow(string userEmail, int courseId);
        Task<IActionResult> CreateRazorpay(IFormCollection form);
        Task<IActionResult> Verify(IFormCollection form);
        Task<IActionResult> CreateOrder(string userEmail, IFormCollection form);
        Task<IActionResult> AddOrderItem(IFormCollection form);
        Task<IActionResult> MarkPaymentPaid(int orderId);
        Task<IActionResult> GetOrder(int orderId);
        Task<IActionResult> GetAllOrders();
        Task<IActionResult> GetPurchaseHistory(string userEmail);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Orders
    {

    }

    public partial class DataBaseLayer
    {

        public async Task<IActionResult> CheckOut(string userEmail, IFormCollection form)
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                // ==================================================
                // 1️⃣ GET USER ID
                // ==================================================
                Guid userId;

                await using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email",
                    con, tran))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null)
                        return BadRequest(new { success = false, message = "User not found" });

                    userId = Guid.Parse(result.ToString()!);
                }

                // ==================================================
                // 2️⃣ GET CART ITEMS
                // ==================================================
                var cartItems = new List<dynamic>();

                await using (var cartCmd = new NpgsqlCommand(@"
            SELECT 
                c.id,
                c.course_name,
                c.course_slug,
                c.course_image,
                ci.saling_price,
                ci.quantity
            FROM cart_items ci
            JOIN courses c ON c.id = ci.course_id
            WHERE ci.user_id = @UserId AND ci.is_active = true
        ", con, tran))
                {
                    cartCmd.Parameters.AddWithValue("@UserId", userId);

                    await using var reader = await cartCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        cartItems.Add(new
                        {
                            CourseId = reader.GetInt32(0),
                            CourseName = reader.GetString(1),
                            CourseSlug = reader.GetString(2),
                            CourseImage = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Price = reader.GetDecimal(4),
                            Quantity = reader.GetInt32(5)
                        });
                    }
                }

                if (!cartItems.Any())
                    return Ok(new { success = false, message = "Cart is empty" });

                // ==================================================
                // 3️⃣ CALCULATE SUBTOTAL
                // ==================================================
                decimal subtotal = cartItems.Sum(x =>
                    (decimal)x.Price * (int)x.Quantity
                );

                // ==================================================
                // 4️⃣ APPLY COUPON (OPTIONAL)
                // ==================================================
                string couponCode = form["couponCode"];
                int? couponId = null;
                decimal couponDiscount = 0;

                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    await using var couponCmd = new NpgsqlCommand(@"
                SELECT id, discount_type, discount_value, min_order_value, max_discount
                FROM coupons
                WHERE code = @Code
                  AND is_active = true
                  AND start_date <= NOW()
                  AND end_date >= NOW()
            ", con, tran);

                    couponCmd.Parameters.AddWithValue("@Code", couponCode);

                    await using var reader = await couponCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return Ok(new { success = false, message = "Invalid or expired coupon" });

                    couponId = reader.GetInt32(0);
                    string discountType = reader.GetString(1);
                    decimal discountValue = reader.GetDecimal(2);
                    decimal minOrder = reader.GetDecimal(3);
                    decimal maxDiscount = reader.GetDecimal(4);

                    if (subtotal < minOrder)
                        return Ok(new
                        {
                            success = false,
                            message = $"Minimum order value should be ₹{minOrder}"
                        });

                    couponDiscount = discountType == "PERCENT"
                        ? Math.Min(subtotal * (discountValue / 100), maxDiscount)
                        : discountValue;
                }

                // ==================================================
                // 5️⃣ FINAL TOTAL
                // ==================================================
                decimal totalAmount = Math.Max(subtotal - couponDiscount, 0);

                // ==================================================
                // 6️⃣ CREATE ORDER (SAVE COUPON DISCOUNT HERE ONLY)
                // ==================================================
                int orderId;

                await using (var orderCmd = new NpgsqlCommand(@"
            INSERT INTO orders
            (user_id, coupon_id, subtotal, discount_amount, total_amount)
            VALUES
            (@UserId, @CouponId, @Subtotal, @Discount, @Total)
            RETURNING id
        ", con, tran))
                {
                    orderCmd.Parameters.AddWithValue("@UserId", userId);
                    orderCmd.Parameters.AddWithValue("@CouponId", (object?)couponId ?? DBNull.Value);
                    orderCmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    orderCmd.Parameters.AddWithValue("@Discount", couponDiscount);
                    orderCmd.Parameters.AddWithValue("@Total", totalAmount);

                    orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
                }

                // ==================================================
                // 7️⃣ INSERT ORDER ITEMS (NO COUPON DISCOUNT HERE)
                // ==================================================
                foreach (var item in cartItems)
                {
                    await using var itemCmd = new NpgsqlCommand(@"
                INSERT INTO order_items
                (order_id, course_id, price, quantity)
                VALUES
                (@OrderId, @CourseId, @Price, @Qty)
            ", con, tran);

                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@CourseId", item.CourseId);
                    itemCmd.Parameters.AddWithValue("@Price", item.Price);
                    itemCmd.Parameters.AddWithValue("@Qty", item.Quantity);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                // ==================================================
                // 8️⃣ CLEAR CART
                // ==================================================
                //await using (var clearCmd = new NpgsqlCommand(
                //    @"DELETE FROM cart_items WHERE user_id = @UserId",
                //    con, tran))
                //{
                //    clearCmd.Parameters.AddWithValue("@UserId", userId);
                //    await clearCmd.ExecuteNonQueryAsync();
                //}

                // ==================================================
                // 9️⃣ COMMIT TRANSACTION
                // ==================================================
                await tran.CommitAsync();

                // ==================================================
                // 🔟 RESPONSE
                // ==================================================
                return Ok(new
                {
                    success = true,
                    order_id = orderId,
                    subtotal,
                    coupon_discount = couponDiscount,
                    total_amount = totalAmount,
                    courses = cartItems
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> BuyNow(string userEmail, int courseId)
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                // ==================================================
                // 1️⃣ GET USER ID
                // ==================================================
                Guid userId;

                await using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email",
                    con, tran))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null)
                        return BadRequest(new { success = false, message = "User not found" });

                    userId = Guid.Parse(result.ToString()!);
                }

                // ==================================================
                // 2️⃣ CHECK IF COURSE ALREADY PURCHASED
                // ==================================================
                await using (var checkCmd = new NpgsqlCommand(@"
            SELECT 1
            FROM order_items oi
            JOIN orders o ON o.id = oi.order_id
            WHERE o.user_id = @UserId
              AND oi.course_id = @CourseId
              AND o.payment_status = 'CONFIRMED'
        ", con, tran))
                {
                    checkCmd.Parameters.AddWithValue("@UserId", userId);
                    checkCmd.Parameters.AddWithValue("@CourseId", courseId);

                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists != null)
                        return BadRequest(new { success = false, message = "Course already purchased" });
                }

                // ==================================================
                // 3️⃣ GET COURSE
                // ==================================================
                BuyNowCourseDto course;

                await using (var courseCmd = new NpgsqlCommand(@"
            SELECT 
                id,
                course_name,
                course_slug,
                course_image,
                saling_price
            FROM courses
            WHERE id = @courseId
              AND is_active = TRUE
        ", con, tran))
                {
                    courseCmd.Parameters.AddWithValue("@courseId", courseId);

                    await using var reader = await courseCmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        return BadRequest(new { success = false, message = "Course not found or inactive" });

                    course = new BuyNowCourseDto
                    {
                        CourseId = reader.GetInt32(0),
                        CourseName = reader.GetString(1),
                        CourseSlug = reader.GetString(2),
                        CourseImage = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Price = reader.GetDecimal(4),
                        Quantity = 1,
                        Discount = 0
                    };
                }

                // ==================================================
                // 4️⃣ CALCULATE TOTALS
                // ==================================================
                decimal subtotal = course.Price;
                decimal couponDiscount = 0;
                decimal totalAmount = subtotal - couponDiscount;
                int? couponId = null;

                // ==================================================
                // 5️⃣ CREATE ORDER
                // ==================================================
                int orderId;

                await using (var orderCmd = new NpgsqlCommand(@"
            INSERT INTO orders
            (user_id, coupon_id, subtotal, discount_amount, total_amount, order_status, payment_status)
            VALUES
            (@UserId, @CouponId, @Subtotal, @Discount, @Total, 'PENDING', 'PENDING')
            RETURNING id
        ", con, tran))
                {
                    orderCmd.Parameters.AddWithValue("@UserId", userId);
                    orderCmd.Parameters.AddWithValue("@CouponId", (object?)couponId ?? DBNull.Value);
                    orderCmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    orderCmd.Parameters.AddWithValue("@Discount", couponDiscount);
                    orderCmd.Parameters.AddWithValue("@Total", totalAmount);

                    orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
                }

                // ==================================================
                // 6️⃣ INSERT ORDER ITEM
                // ==================================================
                await using (var itemCmd = new NpgsqlCommand(@"
            INSERT INTO order_items
            (order_id, course_id, price, discount, quantity)
            VALUES
            (@OrderId, @CourseId, @Price, @Discount, @Qty)
        ", con, tran))
                {
                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@CourseId", course.CourseId);
                    itemCmd.Parameters.AddWithValue("@Price", course.Price);
                    itemCmd.Parameters.AddWithValue("@Discount", course.Discount);
                    itemCmd.Parameters.AddWithValue("@Qty", 1);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                // ==================================================
                // 7️⃣ COMMIT
                // ==================================================
                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    order_id = orderId,
                    total_amount = totalAmount,
                    course
                });
            }
            catch
            {
                await tran.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "Something went wrong while placing order"
                });
            }
        }


        public async Task<IActionResult> CreateRazorpay(IFormCollection form)
        {
            decimal amount = decimal.Parse(form["amount"]);
            int orderId = int.Parse(form["order_id"]);

            var client = new RazorpayClient(_rz.KeyId, _rz.KeySecret);

            var options = new Dictionary<string, object>
    {
        { "amount", amount * 100 }, // paise
        { "currency", _rz.Currency },
        { "receipt", $"order_{orderId}" },
        { "payment_capture", 1 }
    };

            Razorpay.Api.Order razorpayOrder = client.Order.Create(options);

            // 🔥 SAVE RAZORPAY ORDER ID IN DB
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
        UPDATE orders
        SET razorpay_order_id = @rpOrderId
        WHERE id = @orderId
    ", con);

            cmd.Parameters.AddWithValue("@rpOrderId", razorpayOrder["id"].ToString());
            cmd.Parameters.AddWithValue("@orderId", orderId);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                success = true,
                razorpay_order_id = razorpayOrder["id"].ToString(),
                key = _rz.KeyId
            });
        }

        public async Task<IActionResult> Verify(IFormCollection form)
        {
        string razorpayOrderId = form["razorpay_order_id"];
        string razorpayPaymentId = form["razorpay_payment_id"];
        string razorpaySignature = form["razorpay_signature"];
        int orderId = int.Parse(form["order_id"]);

        // ===============================
        // 1️⃣ VERIFY SIGNATURE
        // ===============================
        string payload = razorpayOrderId + "|" + razorpayPaymentId;

        using var hmac = new HMACSHA256(
        Encoding.UTF8.GetBytes(_rz.KeySecret)
        );

        string generatedSignature = BitConverter
        .ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
        .Replace("-", "")
        .ToLower();

        if (generatedSignature != razorpaySignature)
        return BadRequest(new
        {
            success = false,
            message = "Payment verification failed"
        });

        // ===============================
        // 2️⃣ SAVE PAYMENT DETAILS
        // ===============================
        using var con = new NpgsqlConnection(DbConnection);
        await con.OpenAsync();

        using var cmd = new NpgsqlCommand(@"
        UPDATE orders
        SET
            razorpay_payment_id = @paymentId,
            razorpay_signature = @signature
        WHERE id = @orderId
        ", con);

        cmd.Parameters.AddWithValue("@paymentId", razorpayPaymentId);
        cmd.Parameters.AddWithValue("@signature", razorpaySignature);
        cmd.Parameters.AddWithValue("@orderId", orderId);

        await cmd.ExecuteNonQueryAsync();

        // ===============================
        // 3️⃣ MARK PAYMENT PAID + ENROLL USER
        // ===============================
        return await MarkPaymentPaid(orderId);
        }

        public async Task<IActionResult> MarkPaymentPaid(int orderId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                // ===============================
                // 1️⃣ MARK ORDER AS PAID
                // ===============================
                using (var cmd = new NpgsqlCommand(@"
            UPDATE orders
            SET payment_status = 'PAID',
                order_status = 'CONFIRMED'
            WHERE id = @id
        ", con, tran))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // ===============================
                // 2️⃣ GET USER ID
                // ===============================
                Guid userId;
                using (var cmd = new NpgsqlCommand(
                    "SELECT user_id FROM orders WHERE id = @id",
                    con, tran))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);
                    userId = Guid.Parse((await cmd.ExecuteScalarAsync()).ToString());
                }
                // ===============================
                //  CLEAR CART
                // ===============================
                using (var cmd = new NpgsqlCommand(@"
            DELETE FROM cart_items WHERE user_id=@uid
        ", con, tran))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // ===============================
                // 3️⃣ GET COURSES FROM ORDER
                // ===============================
                var courseIds = new List<int>();
                using (var cmd = new NpgsqlCommand(@"
            SELECT course_id
            FROM order_items
            WHERE order_id = @orderId
        ", con, tran))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        courseIds.Add(reader.GetInt32(0));
                }

                // ===============================
                // 4️⃣ ASSIGN EXISTING BATCH ONLY
                // ===============================
                foreach (var courseId in courseIds)
                {
                    // 🔹 FIND ACTIVE BATCH
                    int batchId;
                    using (var cmd = new NpgsqlCommand(@"
                SELECT id
                FROM batches
                WHERE course_id = @courseId
                  AND is_active = TRUE
                LIMIT 1
            ", con, tran))
                    {
                        cmd.Parameters.AddWithValue("@courseId", courseId);
                        var result = await cmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            await tran.RollbackAsync();
                            return new BadRequestObjectResult(new
                            {
                                success = false,
                                message = $"No active batch found for course_id = {courseId}"
                            });
                        }

                        batchId = Convert.ToInt32(result);
                    }

                    // 🔹 ASSIGN USER TO BATCH
                    using (var cmd = new NpgsqlCommand(@"
                INSERT INTO user_batches
                (user_id, course_id, batch_id)
                VALUES
                (@userId, @courseId, @batchId)
                ON CONFLICT (user_id, batch_id) DO NOTHING
            ", con, tran))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@courseId", courseId);
                        cmd.Parameters.AddWithValue("@batchId", batchId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 🔹 ENROLL USER INTO COURSE
                    using (var cmd = new NpgsqlCommand(@"
                INSERT INTO user_courses
                (user_id, course_id, order_id, access_type)
                VALUES
                (@userId, @courseId, @orderId, 'FULL')
                ON CONFLICT (user_id, course_id) DO NOTHING
            ", con, tran))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@courseId", courseId);
                        cmd.Parameters.AddWithValue("@orderId", orderId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Payment successful, user assigned to existing batch"
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

        //public async Task<IActionResult> MarkPaymentPaid(int orderId)
        //{
        //    using var con = new NpgsqlConnection(DbConnection);
        //    await con.OpenAsync();
        //    using var tran = await con.BeginTransactionAsync();

        //    try
        //    {
        //        // ===============================
        //        // 1️⃣ MARK ORDER AS PAID
        //        // ===============================
        //        using (var cmd = new NpgsqlCommand(@"
        //    UPDATE orders
        //    SET payment_status='PAID',
        //        order_status='CONFIRMED'
        //    WHERE id=@id
        //", con, tran))
        //        {
        //            cmd.Parameters.AddWithValue("@id", orderId);
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // ===============================
        //        // 2️⃣ GET USER ID FROM ORDER
        //        // ===============================
        //        Guid userId;

        //        using (var cmd = new NpgsqlCommand(
        //            "SELECT user_id FROM orders WHERE id=@id", con, tran))
        //        {
        //            cmd.Parameters.AddWithValue("@id", orderId);
        //            userId = Guid.Parse((await cmd.ExecuteScalarAsync()).ToString());
        //        }

        //        // ===============================
        //        // 3️⃣ GET COURSES FROM ORDER
        //        // ===============================
        //        var courseIds = new List<int>();

        //        using (var cmd = new NpgsqlCommand(@"
        //    SELECT course_id
        //    FROM order_items
        //    WHERE order_id=@orderId
        //", con, tran))
        //        {
        //            cmd.Parameters.AddWithValue("@orderId", orderId);
        //            using var reader = await cmd.ExecuteReaderAsync();
        //            while (await reader.ReadAsync())
        //                courseIds.Add(reader.GetInt32(0));
        //        }

        //        // ===============================
        //        // 4️⃣ ENROLL USER INTO COURSES
        //        // ===============================
        //        foreach (var courseId in courseIds)
        //        {
        //            using var enrollCmd = new NpgsqlCommand(@"
        //        INSERT INTO user_courses
        //        (user_id, course_id, order_id, access_type)
        //        VALUES
        //        (@UserId, @CourseId, @OrderId, 'FULL')
        //        ON CONFLICT (user_id, course_id) DO NOTHING
        //    ", con, tran);

        //            enrollCmd.Parameters.AddWithValue("@UserId", userId);
        //            enrollCmd.Parameters.AddWithValue("@CourseId", courseId);
        //            enrollCmd.Parameters.AddWithValue("@OrderId", orderId);

        //            await enrollCmd.ExecuteNonQueryAsync();
        //        }

        //        await tran.CommitAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Payment successful & course access granted"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        await tran.RollbackAsync();
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}

        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var order = new Dictionary<string, object>();
                var user = new Dictionary<string, object>();
                var items = new List<Dictionary<string, object>>();

                // ==================================================
                // 1️⃣ ORDER + USER DETAILS
                // ==================================================
              
                await using (var cmd = new NpgsqlCommand(@"
                    SELECT
                        o.id AS order_id,
                        o.subtotal,
                        o.discount_amount,
                        o.total_amount,
                        o.payment_status,
                        o.order_status,
                        u.""Id"" AS user_id,
                        u.""UserName"" AS username,
                        u.""FirstName"",
                        u.""LastName"",
                        u.""Email"" AS email,
                        u.""PhoneNumber"" AS mobile,
                        CONCAT(u.""FirstName"", ' ', u.""LastName"") AS full_name   -- Combined full name
                    FROM orders o
                    LEFT JOIN ""AspNetUsers"" u
                        ON u.""Id"" = o.user_id::text          -- Safe & recommended cast (uuid → text)
                    WHERE o.id = @orderId;
                ", con))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    await using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        return NotFound(new { success = false, message = "Order not found" });

                    order["id"] = reader["order_id"];
                    order["subtotal"] = reader["subtotal"];
                    order["discount_amount"] = reader["discount_amount"];
                    order["total_amount"] = reader["total_amount"];
                    order["payment_status"] = reader["payment_status"];
                    order["order_status"] = reader["order_status"];

                    user["id"] = reader["user_id"];
                    user["username"] = reader["username"];
                    user["firstName"] = reader["FirstName"];
                    user["lastName"] = reader["LastName"];
                    user["fullName"] = reader["full_name"];        // Best for display
                    user["email"] = reader["email"];
                    user["mobile"] = reader["mobile"];
                }

                // ==================================================
                // 2️⃣ ORDER ITEMS + COURSE DETAILS
                // ==================================================
                await using (var cmd = new NpgsqlCommand(@"
            SELECT
                oi.quantity,
                oi.price,
                oi.discount,
                oi.total,

                c.id          AS course_id,
                c.course_name,
                c.course_image,
                c.course_discription,
                c.start_class_date,
                c.mrp_price,
                c.saling_price,
                c.total_lectures,
                c.maximum_lpa,
                c.minimum_lpa,
                c.demo_start_date,
                c.demo_end_date,
                c.course_slug,
                c.duration
            FROM order_items oi
            INNER JOIN courses c ON c.id = oi.course_id
            WHERE oi.order_id = @orderId
        ", con))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["course_id"] = reader["course_id"],
                            ["course_name"] = reader["course_name"],
                            ["course_image"] = reader["course_image"],
                            ["course_discription"] = reader["course_discription"],
                            ["start_class_date"] = reader["start_class_date"],
                            ["mrp_price"] = reader["mrp_price"],
                            ["saling_price"] = reader["saling_price"],
                            ["total_lectures"] = reader["total_lectures"],
                            ["maximum_lpa"] = reader["maximum_lpa"],
                            ["minimum_lpa"] = reader["minimum_lpa"],
                            ["demo_start_date"] = reader["demo_start_date"],
                            ["demo_end_date"] = reader["demo_end_date"],
                            ["course_slug"] = reader["course_slug"],
                            ["duration"] = reader["duration"],
                            ["price"] = reader["price"],
                            ["discount"] = reader["discount"],
                            ["quantity"] = reader["quantity"],
                            ["total"] = reader["total"]
                        });
                    }
                }

                // ==================================================
                // 3️⃣ FINAL RESPONSE
                // ==================================================
                return Ok(new
                {
                    success = true,
                    order,
                    user,
                    items
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //public async Task<IActionResult> GetAllOrders()
        //{
        //    try
        //    {
        //        using var con = new NpgsqlConnection(DbConnection);
        //        await con.OpenAsync();

        //        string query = @"
        //            SELECT *
        //            FROM orders
        //            WHERE order_status = 'CONFIRMED'
        //              AND payment_status = 'PAID'
        //            ORDER BY id DESC
        //        ";


        //        var list = new List<Dictionary<string, object>>();

        //        using var cmd = new NpgsqlCommand(query, con);
        //        using var reader = await cmd.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            list.Add(new Dictionary<string, object>
        //    {
        //        { "id", reader["id"] },
        //        { "user_id", reader["user_id"] },
        //        { "subtotal", reader["subtotal"] },
        //        { "discount_amount", reader["discount_amount"] },
        //        { "total_amount", reader["total_amount"] },
        //        { "payment_status", reader["payment_status"] },
        //        { "order_status", reader["order_status"] },
        //        { "created_at", reader["created_at"] }
        //    });
        //        }

        //        return new OkObjectResult(new { success = true, data = list });
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BadRequestObjectResult(new { success = false, message = ex.Message });
        //    }
        //}


        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = @"
            SELECT
                o.id AS order_id,
                o.user_id,
                o.subtotal,
                o.discount_amount,
                o.total_amount,
                o.payment_status,
                o.order_status,
                o.created_at,

                -- User Info
                u.""Id"" AS user_id_str,
                u.""UserName"" AS username,
                u.""FirstName"",
                u.""LastName"",
                u.""Email"" AS email,
                u.""PhoneNumber"" AS mobile,

                -- Order Items + Course Details
                oi.quantity,
                oi.price,
                oi.discount,
                oi.total AS item_total,
                c.id AS course_id,
                c.course_name,
                c.course_image,
                c.course_discription,
                c.saling_price,
                c.mrp_price,
                c.duration,
                c.course_slug
            FROM orders o
            LEFT JOIN ""AspNetUsers"" u 
                ON u.""Id"" = o.user_id::text
            LEFT JOIN order_items oi 
                ON oi.order_id = o.id
            LEFT JOIN courses c 
                ON c.id = oi.course_id
            WHERE o.order_status = 'CONFIRMED'
              AND o.payment_status = 'PAID'
            ORDER BY o.id DESC, oi.id;";

                var list = new List<Dictionary<string, object>>();

                await using var cmd = new NpgsqlCommand(query, con);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var orderDict = new Dictionary<string, object>
                    {
                        ["id"] = reader["order_id"],
                        ["user_id"] = reader["user_id"],
                        ["subtotal"] = reader["subtotal"],
                        ["discount_amount"] = reader["discount_amount"],
                        ["total_amount"] = reader["total_amount"],
                        ["payment_status"] = reader["payment_status"],
                        ["order_status"] = reader["order_status"],
                        ["created_at"] = reader["created_at"],

                        // User Info - Updated with FirstName & LastName
                        ["user"] = new Dictionary<string, object>
                        {
                            ["id"] = reader["user_id_str"],
                            ["username"] = reader["username"],
                            ["firstName"] = reader["FirstName"],
                            ["lastName"] = reader["LastName"],
                            ["fullName"] = reader.IsDBNull("FirstName") && reader.IsDBNull("LastName")
                                ? reader["username"]?.ToString()
                                : $"{reader["FirstName"]} {reader["LastName"]}".Trim(),
                            ["email"] = reader["email"],
                            ["mobile"] = reader["mobile"]
                        },

                        // Course / Item Info (one row per item)
                        ["items"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["course_id"] = reader["course_id"],
                        ["course_name"] = reader["course_name"],
                        ["course_image"] = reader["course_image"],
                        ["course_discription"] = reader["course_discription"],
                        ["quantity"] = reader["quantity"],
                        ["price"] = reader["price"],
                        ["discount"] = reader["discount"],
                        ["total"] = reader["item_total"],
                        ["saling_price"] = reader["saling_price"],
                        ["mrp_price"] = reader["mrp_price"],
                        ["duration"] = reader["duration"],
                        ["course_slug"] = reader["course_slug"]
                    }
                }
                    };

                    list.Add(orderDict);
                }

                return Ok(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"DB Error in GetAllOrders: {ex.Message}" });
            }
        }

        /// <summary>Completed orders (paid + confirmed) for the given account email, grouped with line items.</summary>
        public async Task<IActionResult> GetPurchaseHistory(string userEmail)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
                return new BadRequestObjectResult(new { success = false, message = "User email is required" });

            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                Guid userId;
                await using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE LOWER(""Email"") = LOWER(@Email) LIMIT 1", con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail.Trim());
                    var idObj = await userCmd.ExecuteScalarAsync();
                    if (idObj == null)
                        return new NotFoundObjectResult(new { success = false, message = "User not found" });
                    userId = Guid.Parse(idObj.ToString()!);
                }

                const string query = @"
SELECT
    o.id AS order_id,
    o.created_at,
    o.subtotal,
    o.discount_amount,
    o.total_amount,
    o.payment_status,
    o.order_status,
    o.razorpay_order_id,
    o.razorpay_payment_id,
    oi.id AS order_item_id,
    oi.course_id,
    oi.price AS item_price,
    oi.discount AS item_discount,
    oi.quantity,
    oi.total AS line_total,
    c.course_name,
    c.course_image,
    c.course_slug,
    c.duration
FROM orders o
INNER JOIN order_items oi ON oi.order_id = o.id
LEFT JOIN courses c ON c.id = oi.course_id
WHERE o.user_id = @userId
  AND o.order_status = 'CONFIRMED'
  AND (o.payment_status = 'PAID' OR o.payment_status = 'CONFIRMED')
ORDER BY o.created_at DESC, oi.id;";

                var orderMeta = new Dictionary<int, Dictionary<string, object?>>();
                var orderItems = new Dictionary<int, List<object>>();

                await using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var orderId = Convert.ToInt32(reader["order_id"]);
                        if (!orderMeta.ContainsKey(orderId))
                        {
                            orderMeta[orderId] = new Dictionary<string, object?>
                            {
                                ["orderId"] = orderId,
                                ["createdAt"] = reader["created_at"],
                                ["subtotal"] = reader["subtotal"],
                                ["discountAmount"] = reader["discount_amount"],
                                ["totalAmount"] = reader["total_amount"],
                                ["paymentStatus"] = reader["payment_status"],
                                ["orderStatus"] = reader["order_status"],
                                ["razorpayOrderId"] = reader.IsDBNull(reader.GetOrdinal("razorpay_order_id")) ? null : reader["razorpay_order_id"],
                                ["razorpayPaymentId"] = reader.IsDBNull(reader.GetOrdinal("razorpay_payment_id")) ? null : reader["razorpay_payment_id"]
                            };
                            orderItems[orderId] = new List<object>();
                        }

                        orderItems[orderId].Add(new
                        {
                            orderItemId = reader["order_item_id"],
                            courseId = reader["course_id"],
                            courseName = reader.IsDBNull(reader.GetOrdinal("course_name")) ? null : reader["course_name"],
                            courseImage = reader.IsDBNull(reader.GetOrdinal("course_image")) ? null : reader["course_image"],
                            courseSlug = reader.IsDBNull(reader.GetOrdinal("course_slug")) ? null : reader["course_slug"],
                            duration = reader.IsDBNull(reader.GetOrdinal("duration")) ? null : reader["duration"],
                            quantity = reader["quantity"],
                            price = reader["item_price"],
                            discount = reader["item_discount"],
                            lineTotal = reader["line_total"]
                        });
                    }
                }

                var purchases = orderMeta
                    .OrderByDescending(kv => Convert.ToDateTime(kv.Value["createdAt"]))
                    .Select(kv => new
                    {
                        order = kv.Value,
                        items = orderItems[kv.Key]
                    })
                    .ToList();

                return new OkObjectResult(new
                {
                    success = true,
                    totalOrders = purchases.Count,
                    purchases
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }
    }
}
