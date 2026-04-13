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

                const string query = @"
SELECT
    -- Student stats from USER role
    COALESCE(SUM(CASE WHEN u.""IsActive"" = TRUE THEN 1 ELSE 0 END), 0) AS active_students,
    COALESCE(SUM(CASE WHEN u.""IsActive"" = FALSE THEN 1 ELSE 0 END), 0) AS inactive_students,
    COUNT(u.""Id"") AS total_students,

    -- Orders
    (SELECT COUNT(*) FROM orders) AS total_orders,
    (SELECT COUNT(*) FROM orders WHERE order_status = 'CONFIRMED' AND (payment_status = 'PAID' OR payment_status = 'CONFIRMED')) AS total_confirmed_orders,

    -- Categories
    (SELECT COUNT(*) FROM categories) AS total_categories,
    (SELECT COUNT(*) FROM languages) AS total_languages,
    (SELECT COUNT(*) FROM faculties) AS total_faculties,
    (SELECT COUNT(*) FROM batches) AS total_batches,
    (SELECT COUNT(*) FROM live_classes) AS total_live_classes,

    -- Purchased courses (distinct mapped courses)
    (SELECT COUNT(DISTINCT course_id) FROM user_courses WHERE is_active = TRUE) AS total_purchased_courses
FROM ""AspNetUsers"" u
INNER JOIN ""AspNetUserRoles"" ur ON ur.""UserId"" = u.""Id""
INNER JOIN ""AspNetRoles"" r ON r.""Id"" = ur.""RoleId""
WHERE UPPER(r.""Name"") = 'USER';";

                await using var cmd = new NpgsqlCommand(query, con);
                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return new OkObjectResult(new { success = true, report = new { } });
                }

                var activeStudents = Convert.ToInt32(reader["active_students"]);
                var inactiveStudents = Convert.ToInt32(reader["inactive_students"]);
                var totalStudents = Convert.ToInt32(reader["total_students"]);
                var totalOrders = Convert.ToInt32(reader["total_orders"]);
                var totalConfirmedOrders = Convert.ToInt32(reader["total_confirmed_orders"]);
                var totalCategories = Convert.ToInt32(reader["total_categories"]);
                var totalLanguages = Convert.ToInt32(reader["total_languages"]);
                var totalFaculties = Convert.ToInt32(reader["total_faculties"]);
                var totalBatches = Convert.ToInt32(reader["total_batches"]);
                var totalLiveClasses = Convert.ToInt32(reader["total_live_classes"]);
                var totalPurchasedCourses = Convert.ToInt32(reader["total_purchased_courses"]);
                var grandTotalEntities =
                    totalStudents + totalOrders + totalCategories + totalLanguages +
                    totalFaculties + totalBatches + totalLiveClasses + totalPurchasedCourses;

                return new OkObjectResult(new
                {
                    success = true,
                    report = new
                    {
                        students = new
                        {
                            total = totalStudents,
                            active = activeStudents,
                            inactive = inactiveStudents
                        },
                        orders = new
                        {
                            total = totalOrders,
                            confirmed = totalConfirmedOrders
                        },
                        categories = new
                        {
                            total = totalCategories
                        },
                        languages = new
                        {
                            total = totalLanguages
                        },
                        faculties = new
                        {
                            total = totalFaculties
                        },
                        batches = new
                        {
                            total = totalBatches
                        },
                        liveClasses = new
                        {
                            total = totalLiveClasses
                        },
                        purchasedCourses = new
                        {
                            total = totalPurchasedCourses
                        },
                        totals = new
                        {
                            grandTotal = grandTotalEntities
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = $"DB Error in GetDashboardReport: {ex.Message}"
                });
            }
        }
    }

}