using CareerCracker.BusinessLayer;
using CareerCracker.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_AdminOrder
    {
        Task<List<AdminOrderModel>> GetAllAdminOrders();
        Task<IActionResult> GetDashboardReport();
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_AdminOrder { }

    public partial class DataBaseLayer
    {
        public async Task<List<AdminOrderModel>> GetAllAdminOrders()
        {
            var list = new List<AdminOrderModel>();

            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var query = @"
            SELECT
                o.id,
                o.user_id,
                o.coupon_id,
                o.subtotal,
                o.discount_amount,
                o.total_amount,
                o.payment_status,
                o.order_status,
                o.created_at,
                u.""UserName"",
                u.""Email"",
                u.""PhoneNumber""
            FROM orders o
            LEFT JOIN ""AspNetUsers"" u
                ON u.""Id"" = o.user_id::text          -- Cast uuid → text (safer for Identity table)
            WHERE o.user_id IS NOT NULL
            ORDER BY o.created_at DESC;";

                using var cmd = new NpgsqlCommand(query, con);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new AdminOrderModel
                    {
                        Id = reader.GetInt32("id"),
                        UserId = reader["user_id"]?.ToString(),
                        CouponId = reader.IsDBNull("coupon_id")
                            ? null
                            : reader.GetInt32("coupon_id"),
                        Subtotal = reader.GetDecimal("subtotal"),
                        DiscountAmount = reader.GetDecimal("discount_amount"),
                        TotalAmount = reader.GetDecimal("total_amount"),
                        PaymentStatus = reader["payment_status"]?.ToString(),
                        OrderStatus = reader["order_status"]?.ToString(),
                        CreatedAt = reader.GetDateTime("created_at"),

                        // User Info from AspNetUsers
                        Name = reader["UserName"]?.ToString(),
                        Email = reader["Email"]?.ToString(),
                        Mobile = reader["PhoneNumber"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the full exception in production (don't just throw generic message)
                throw new Exception($"DB Error in GetAllAdminOrders: {ex.Message}", ex);
            }

            return list;
        }

        public async Task<IActionResult> GetDashboardReport()
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // =========================================
                // 1️⃣ MAIN DASHBOARD STATS
                // =========================================
                const string query = @"
SELECT
    -- Students
    COALESCE(SUM(CASE WHEN u.""IsActive"" = TRUE THEN 1 ELSE 0 END), 0) AS active_students,
    COALESCE(SUM(CASE WHEN u.""IsActive"" = FALSE THEN 1 ELSE 0 END), 0) AS inactive_students,
    COUNT(u.""Id"") AS total_students,

    -- Orders
    (SELECT COUNT(*) FROM orders) AS total_orders,
    (SELECT COUNT(*) FROM orders 
        WHERE order_status = 'CONFIRMED' 
        AND (payment_status = 'PAID' OR payment_status = 'CONFIRMED')
    ) AS total_confirmed_orders,

    -- Masters
    (SELECT COUNT(*) FROM categories) AS total_categories,
    (SELECT COUNT(*) FROM languages) AS total_languages,

    -- ✅ FIXED FACULTY COUNT (USING ADMIN ROLE)
    (SELECT COUNT(*) 
     FROM ""AspNetUsers"" u2
     INNER JOIN ""AspNetUserRoles"" ur2 ON ur2.""UserId"" = u2.""Id""
     INNER JOIN ""AspNetRoles"" r2 ON r2.""Id"" = ur2.""RoleId""
     WHERE u2.status = TRUE
       AND r2.""NormalizedName"" = 'ADMIN'
    ) AS total_faculties,

    (SELECT COUNT(*) FROM batches) AS total_batches,
    (SELECT COUNT(*) FROM live_classes) AS total_live_classes,

    (SELECT COUNT(DISTINCT course_id) 
     FROM user_courses WHERE is_active = TRUE) AS total_purchased_courses

FROM ""AspNetUsers"" u
INNER JOIN ""AspNetUserRoles"" ur ON ur.""UserId"" = u.""Id""
INNER JOIN ""AspNetRoles"" r ON r.""Id"" = ur.""RoleId""
WHERE r.""NormalizedName"" = 'USER';";

                await using var cmd = new NpgsqlCommand(query, con);
                await using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Ok(new { success = true, report = new { } });
                }

                var report = new
                {
                    students = new
                    {
                        total = Convert.ToInt32(reader["total_students"]),
                        active = Convert.ToInt32(reader["active_students"]),
                        inactive = Convert.ToInt32(reader["inactive_students"])
                    },
                    orders = new
                    {
                        total = Convert.ToInt32(reader["total_orders"]),
                        confirmed = Convert.ToInt32(reader["total_confirmed_orders"])
                    },
                    categories = new { total = Convert.ToInt32(reader["total_categories"]) },
                    languages = new { total = Convert.ToInt32(reader["total_languages"]) },
                    faculties = new { total = Convert.ToInt32(reader["total_faculties"]) },
                    batches = new { total = Convert.ToInt32(reader["total_batches"]) },
                    liveClasses = new { total = Convert.ToInt32(reader["total_live_classes"]) },
                    purchasedCourses = new { total = Convert.ToInt32(reader["total_purchased_courses"]) }
                };

                await reader.CloseAsync();

                // =========================================
                // 2️⃣ RECENT ORDERS
                // =========================================
                var recentOrders = new List<object>();

                string orderQuery = @"
SELECT 
    o.id,
    o.total_amount,
    o.payment_status,
    o.order_status,
    o.created_at,
    u.""UserName"",
    u.""Email""
FROM orders o
LEFT JOIN ""AspNetUsers"" u ON u.""Id"" = o.user_id::text
ORDER BY o.created_at DESC
LIMIT 5;";

                await using (var orderCmd = new NpgsqlCommand(orderQuery, con))
                await using (var orderReader = await orderCmd.ExecuteReaderAsync())
                {
                    while (await orderReader.ReadAsync())
                    {
                        recentOrders.Add(new
                        {
                            id = orderReader["id"],
                            user = orderReader["UserName"]?.ToString(),
                            email = orderReader["Email"]?.ToString(),
                            amount = orderReader["total_amount"],
                            payment_status = orderReader["payment_status"]?.ToString(),
                            order_status = orderReader["order_status"]?.ToString(),
                            created_at = orderReader["created_at"]
                        });
                    }
                }

                // =========================================
                // 3️⃣ RECENT STUDENTS
                // =========================================
                var recentStudents = new List<object>();

                string studentQuery = @"
SELECT 
    u.""Id"",
    u.""FirstName"",
    u.""LastName"",
    u.""Email"",
    u.""PhoneNumber"",
    u.created_at
FROM ""AspNetUsers"" u
INNER JOIN ""AspNetUserRoles"" ur ON ur.""UserId"" = u.""Id""
INNER JOIN ""AspNetRoles"" r ON r.""Id"" = ur.""RoleId""
WHERE r.""NormalizedName"" = 'USER'
ORDER BY u.created_at DESC
LIMIT 5;";

                await using (var studentCmd = new NpgsqlCommand(studentQuery, con))
                await using (var studentReader = await studentCmd.ExecuteReaderAsync())
                {
                    while (await studentReader.ReadAsync())
                    {
                        recentStudents.Add(new
                        {
                            id = studentReader["Id"]?.ToString(),
                            name = $"{studentReader["FirstName"]} {studentReader["LastName"]}",
                            email = studentReader["Email"]?.ToString(),
                            phone = studentReader["PhoneNumber"]?.ToString(),
                            created_at = studentReader["created_at"]
                        });
                    }
                }

                // =========================================
                // ✅ FINAL RESPONSE
                // =========================================
                return Ok(new
                {
                    success = true,
                    report,
                    recent_orders = recentOrders,
                    recent_students = recentStudents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"DB Error in GetDashboardReport: {ex.Message}"
                });
            }
        }
    }

}