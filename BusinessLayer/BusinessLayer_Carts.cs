using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Carts
    {
        Task<IActionResult> AddToCart(int courseId, string userEmail, string clientIp);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Carts { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> AddToCart(int courseId, string userEmail, string clientIp)
        {
            return await _dataBaseLayer.AddToCart(courseId, userEmail, clientIp);
        }
    }
}
