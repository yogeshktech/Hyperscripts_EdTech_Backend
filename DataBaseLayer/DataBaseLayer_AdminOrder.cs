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
    }

    public partial interface IBusinessLayer : IBusinessLayer_AdminOrder { }

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
    }

}