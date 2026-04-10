using CareerCracker.BusinessLayer;
using CareerCracker.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
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
    o.id AS order_id,
    o.subtotal,
    o.discount_amount,
    o.total_amount,
    o.payment_status,
    o.order_status,
    o.created_at,

    u.""Id""          AS user_id,
    u.""UserName""    AS name,
    u.""Email""       AS email,
    u.""PhoneNumber"" AS mobile

FROM orders o
JOIN ""AspNetUsers"" u
    ON u.""Id"" = o.user_id::uuid   -- ✅ MUST CAST

ORDER BY o.created_at DESC;
        ";

                using var cmd = new NpgsqlCommand(query, con);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new AdminOrderModel
                    {
                        OrderId = reader.GetInt32(reader.GetOrdinal("order_id")),
                        UserId = reader.GetGuid(reader.GetOrdinal("user_id")), // ✅ correct

                        Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),

                        PaymentStatus = reader["payment_status"]?.ToString(),
                        OrderStatus = reader["order_status"]?.ToString(),

                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),

                        Name = reader["name"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        Mobile = reader["mobile"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error: " + ex.Message);
            }

            return list;
        }
    }

}