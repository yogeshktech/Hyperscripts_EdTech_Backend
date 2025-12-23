using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Npgsql;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;


namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Orders
    {
        Task<IActionResult> CheckOut(string userEmail, IFormCollection form);
        Task<IActionResult> CreateRazorpay(IFormCollection form);
        Task<IActionResult> Verify(IFormCollection form);
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
        //public async Task<IActionResult> CheckOut(string userEmail, IFormCollection form)
        //{
        //    using var con = new NpgsqlConnection(DbConnection);
        //    await con.OpenAsync();
        //    using var tran = await con.BeginTransactionAsync();

        //    try
        //    {
        //        // ===============================
        //        // 1️⃣ GET USER ID
        //        // ===============================
        //        Guid userId;

        //        using (var userCmd = new NpgsqlCommand(
        //            @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email", con, tran))
        //        {
        //            userCmd.Parameters.AddWithValue("@Email", userEmail);
        //            var result = await userCmd.ExecuteScalarAsync();

        //            if (result == null)
        //                return BadRequest(new { success = false, message = "User not found" });

        //            userId = Guid.Parse(result.ToString());
        //        }

        //        // ===============================
        //        // 2️⃣ GET CART ITEMS
        //        // ===============================
        //        var cartItems = new List<(int courseId, decimal price, decimal discount, int qty)>();

        //        using (var cartCmd = new NpgsqlCommand(@"
        //    SELECT course_id, saling_price, discount, quantity
        //    FROM cart_items
        //    WHERE user_id=@UserId AND is_active=true", con, tran))
        //        {
        //            cartCmd.Parameters.AddWithValue("@UserId", userId);

        //            using var reader = await cartCmd.ExecuteReaderAsync();
        //            while (await reader.ReadAsync())
        //            {
        //                cartItems.Add((
        //                    reader.GetInt32(0),
        //                    reader.GetDecimal(1),
        //                    reader.GetDecimal(2),
        //                    reader.GetInt32(3)
        //                ));
        //            }
        //        }

        //        if (!cartItems.Any())
        //            return Ok(new { success = false, message = "Cart is empty" });

        //        // ===============================
        //        // 3️⃣ CALCULATE SUBTOTAL
        //        // ===============================
        //        decimal subtotal = 0;

        //        foreach (var item in cartItems)
        //            subtotal += item.price * item.qty;

        //        // ===============================
        //        // 4️⃣ APPLY COUPON (OPTIONAL)
        //        // ===============================
        //        string couponCode = form["couponCode"];
        //        int? couponId = null;
        //        decimal couponDiscount = 0;

        //        if (!string.IsNullOrWhiteSpace(couponCode))
        //        {
        //            using var couponCmd = new NpgsqlCommand(@"
        //        SELECT id, discount_type, discount_value, min_order_value, max_discount
        //        FROM coupons
        //        WHERE code=@Code
        //          AND is_active=true
        //          AND start_date<=NOW()
        //          AND end_date>=NOW()", con, tran);

        //            couponCmd.Parameters.AddWithValue("@Code", couponCode);

        //            using var reader = await couponCmd.ExecuteReaderAsync();
        //            if (!reader.Read())
        //                return Ok(new { success = false, message = "Invalid or expired coupon" });

        //            couponId = reader.GetInt32(0);
        //            string type = reader.GetString(1);
        //            decimal value = reader.GetDecimal(2);
        //            decimal minOrder = reader.GetDecimal(3);
        //            decimal maxDiscount = reader.GetDecimal(4);

        //            if (subtotal < minOrder)
        //                return Ok(new
        //                {
        //                    success = false,
        //                    message = $"Minimum order value should be ₹{minOrder}"
        //                });

        //            if (type == "PERCENT")
        //            {
        //                couponDiscount = subtotal * (value / 100);
        //                if (couponDiscount > maxDiscount)
        //                    couponDiscount = maxDiscount;
        //            }
        //            else // fixed
        //            {
        //                couponDiscount = value;
        //            }
        //        }

        //        // ===============================
        //        // 5️⃣ FINAL TOTAL
        //        // ===============================
        //        decimal totalAmount = subtotal - couponDiscount;
        //        if (totalAmount < 0) totalAmount = 0;

        //        // ===============================
        //        // 6️⃣ CREATE ORDER
        //        // ===============================
        //        int orderId;

        //        using (var orderCmd = new NpgsqlCommand(@"
        //    INSERT INTO orders
        //    (user_id, coupon_id, subtotal, discount_amount, total_amount)
        //    VALUES
        //    (@UserId, @CouponId, @Subtotal, @Discount, @Total)
        //    RETURNING id", con, tran))
        //        {
        //            orderCmd.Parameters.AddWithValue("@UserId", userId);
        //            orderCmd.Parameters.AddWithValue("@CouponId",
        //                (object?)couponId ?? DBNull.Value);
        //            orderCmd.Parameters.AddWithValue("@Subtotal", subtotal);
        //            orderCmd.Parameters.AddWithValue("@Discount", couponDiscount);
        //            orderCmd.Parameters.AddWithValue("@Total", totalAmount);

        //            orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
        //        }

        //        // ===============================
        //        // 7️⃣ INSERT ORDER ITEMS
        //        // ===============================
        //        foreach (var item in cartItems)
        //        {
        //            using var itemCmd = new NpgsqlCommand(@"
        //        INSERT INTO order_items
        //        (order_id, course_id, price, discount, quantity)
        //        VALUES
        //        (@OrderId, @CourseId, @Price, @Discount, @Qty)", con, tran);

        //            itemCmd.Parameters.AddWithValue("@OrderId", orderId);
        //            itemCmd.Parameters.AddWithValue("@CourseId", item.courseId);
        //            itemCmd.Parameters.AddWithValue("@Price", item.price);
        //            itemCmd.Parameters.AddWithValue("@Discount", item.discount);
        //            itemCmd.Parameters.AddWithValue("@Qty", item.qty);

        //            await itemCmd.ExecuteNonQueryAsync();
        //        }

        //        // ===============================
        //        // 8️⃣ CLEAR CART
        //        // ===============================
        //        using (var clearCmd = new NpgsqlCommand(
        //            "DELETE FROM cart_items WHERE user_id=@UserId", con, tran))
        //        {
        //            clearCmd.Parameters.AddWithValue("@UserId", userId);
        //            await clearCmd.ExecuteNonQueryAsync();
        //        }

        //        await tran.CommitAsync();

        //        // ===============================
        //        // 9️⃣ RESPONSE
        //        // ===============================
        //        return Ok(new
        //        {
        //            success = true,
        //            order_id = orderId,
        //            subtotal,
        //            coupon_discount = couponDiscount,
        //            total_amount = totalAmount
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        await tran.RollbackAsync();
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}

        public async Task<IActionResult> CheckOut(string userEmail, IFormCollection form)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                // ==================================================
                // 1️⃣ GET USER ID
                // ==================================================
                Guid userId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email",
                    con, tran))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);
                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return BadRequest(new { success = false, message = "User not found" });

                    userId = Guid.Parse(result.ToString());
                }

                // ==================================================
                // 2️⃣ GET CART + COURSE DETAILS (SINGLE QUERY ✅)
                // ==================================================
                var cartItems = new List<dynamic>();

                using (var cartCmd = new NpgsqlCommand(@"
        SELECT 
            c.id,
            c.course_name,
            c.course_slug,
            c.course_image,
            ci.saling_price,
            ci.discount,
            ci.quantity
        FROM cart_items ci
        JOIN courses c ON c.id = ci.course_id
        WHERE ci.user_id = @UserId AND ci.is_active = true
        ", con, tran))
                {
                    cartCmd.Parameters.AddWithValue("@UserId", userId);

                    using var reader = await cartCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        cartItems.Add(new
                        {
                            CourseId = reader.GetInt32(0),
                            CourseName = reader.GetString(1),
                            CourseSlug = reader.GetString(2),
                            CourseImage = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Price = reader.GetDecimal(4),
                            Discount = reader.GetDecimal(5),
                            Quantity = reader.GetInt32(6),
                            ItemTotal = (reader.GetDecimal(4) - reader.GetDecimal(5)) * reader.GetInt32(6)
                        });
                    }
                }

                if (!cartItems.Any())
                    return Ok(new { success = false, message = "Cart is empty" });

                // ==================================================
                // 3️⃣ CALCULATE SUBTOTAL
                // ==================================================
                decimal subtotal = cartItems.Sum(x => (decimal)x.Price * (int)x.Quantity);

                // ==================================================
                // 4️⃣ APPLY COUPON (OPTIONAL)
                // ==================================================
                string couponCode = form["couponCode"];
                int? couponId = null;
                decimal couponDiscount = 0;

                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    using var couponCmd = new NpgsqlCommand(@"
            SELECT id, discount_type, discount_value, min_order_value, max_discount
            FROM coupons
            WHERE code = @Code
              AND is_active = true
              AND start_date <= NOW()
              AND end_date >= NOW()
            ", con, tran);

                    couponCmd.Parameters.AddWithValue("@Code", couponCode);

                    using var reader = await couponCmd.ExecuteReaderAsync();
                    if (!reader.Read())
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

                    if (discountType == "PERCENT")
                    {
                        couponDiscount = subtotal * (discountValue / 100);
                        if (couponDiscount > maxDiscount)
                            couponDiscount = maxDiscount;
                    }
                    else
                    {
                        couponDiscount = discountValue;
                    }
                }

                // ==================================================
                // 5️⃣ FINAL TOTAL
                // ==================================================
                decimal totalAmount = subtotal - couponDiscount;
                if (totalAmount < 0) totalAmount = 0;

                // ==================================================
                // 6️⃣ CREATE ORDER
                // ==================================================
                int orderId;

                using (var orderCmd = new NpgsqlCommand(@"
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
                // 7️⃣ INSERT ORDER ITEMS
                // ==================================================
                foreach (var item in cartItems)
                {
                    using var itemCmd = new NpgsqlCommand(@"
            INSERT INTO order_items
            (order_id, course_id, price, discount, quantity)
            VALUES
            (@OrderId, @CourseId, @Price, @Discount, @Qty)
            ", con, tran);

                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@CourseId", item.CourseId);
                    itemCmd.Parameters.AddWithValue("@Price", item.Price);
                    itemCmd.Parameters.AddWithValue("@Discount", item.Discount);
                    itemCmd.Parameters.AddWithValue("@Qty", item.Quantity);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                // ==================================================
                // 8️⃣ CLEAR CART
                // ==================================================
                using (var clearCmd = new NpgsqlCommand(
                    @"DELETE FROM cart_items WHERE user_id = @UserId", con, tran))
                {
                    clearCmd.Parameters.AddWithValue("@UserId", userId);
                    await clearCmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();

                // ==================================================
                // 9️⃣ FINAL RESPONSE (WITH COURSE DETAILS ✅)
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
