using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Orders
    {
        Task<IActionResult> CreateOrder(string userEmail,IFormCollection form);
        Task<IActionResult> AddOrderItem(IFormCollection form);
        Task<IActionResult> GetOrder(int orderId);
        Task<IActionResult> GetAllOrders();
        Task<IActionResult> UpdateOrderStatus(IFormCollection form);
        Task<IActionResult> UpdatePaymentStatus(IFormCollection form);
        Task<IActionResult> DeleteOrder(int id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Orders { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> CreateOrder(string userEmail, IFormCollection form)
        {
            return await _dataBaseLayer.CreateOrder(userEmail, form);
        }

        public async Task<IActionResult> AddOrderItem(IFormCollection form)
        {
            return await _dataBaseLayer.AddOrderItem(form);
        }

        public async Task<IActionResult> GetOrder(int orderId)
        {
            return await _dataBaseLayer.GetOrder(orderId);
        }

        public async Task<IActionResult> GetAllOrders()
        {
            return await _dataBaseLayer.GetAllOrders();
        }

        public async Task<IActionResult> UpdateOrderStatus(IFormCollection form)
        {
            return await _dataBaseLayer.UpdateOrderStatus(form);
        }

        public async Task<IActionResult> UpdatePaymentStatus(IFormCollection form)
        {
            return await _dataBaseLayer.UpdatePaymentStatus(form);
        }

        public async Task<IActionResult> DeleteOrder(int id)
        {
            return await _dataBaseLayer.DeleteOrder(id);
        }

    }
}
