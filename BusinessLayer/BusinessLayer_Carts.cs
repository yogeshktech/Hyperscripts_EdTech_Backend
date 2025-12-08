using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Carts
    {
        Task<IActionResult> AddToCart(int courseId, string userEmail, string clientIp);
        Task<IActionResult> GetToCart( string userEmail, string clientIp);
        Task<IActionResult> DeleteCart(int cartId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Carts { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> AddToCart(int courseId, string userEmail, string clientIp)
        {
            return await _dataBaseLayer.AddToCart(courseId, userEmail, clientIp);
        }

        public async Task<IActionResult> GetToCart(string userEmail, string clientIp)
        {
            return await _dataBaseLayer.GetToCart( userEmail, clientIp);
        }

        public async Task<IActionResult> DeleteCart(int cartId)
        {
            return await _dataBaseLayer.DeleteCart( cartId);
        }
    }
}
