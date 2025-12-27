using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Orders
    {
        Task<IActionResult> CheckOut(string userEmail, IFormCollection form);
        Task<IActionResult> BuyNow(string userEmail, IFormCollection form);
        Task<IActionResult> CreateRazorpay(IFormCollection form);
        Task<IActionResult> Verify(IFormCollection form);
        Task<IActionResult> MarkPaymentPaid(int orderId);
        Task<IActionResult> CreateOrder(string userEmail,IFormCollection form);
        Task<IActionResult> AddOrderItem(IFormCollection form);
        

        Task<IActionResult> GetOrder(int orderId);
        Task<IActionResult> GetAllOrders();
    }

    public partial interface IBusinessLayer : IBusinessLayer_Orders { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> CheckOut(string userEmail, IFormCollection form)
        {
            return await _dataBaseLayer.CheckOut(userEmail,form);
        }
        public async Task<IActionResult> BuyNow(string userEmail, IFormCollection form)
        {
            return await _dataBaseLayer.BuyNow(userEmail, form);
        }

        public async Task<IActionResult> CreateRazorpay(IFormCollection form)
        {
            return await _dataBaseLayer.CreateRazorpay(form);
        }

        public async Task<IActionResult> Verify(IFormCollection form)
        {
            return await _dataBaseLayer.Verify(form);
        }

        public async Task<IActionResult> MarkPaymentPaid(int orderId)
        {
            return await _dataBaseLayer.MarkPaymentPaid(orderId);
        }
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

    }
}
