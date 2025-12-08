using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Coupon
    {
        Task<IActionResult> AddCoupon(IFormCollection form);
        Task<IActionResult> GetAllCoupons();
        Task<IActionResult> GetCouponById(int id);
        Task<IActionResult> UpdateCoupon(int id, IFormCollection form);
        Task<IActionResult> DeleteCoupon(int id);
        Task<IActionResult> ToggleCoupon(int id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Coupon { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> AddCoupon(IFormCollection form)
        {
            return await _dataBaseLayer.AddCoupon(form);
        }

        public async Task<IActionResult> GetAllCoupons()
        {
            return await _dataBaseLayer.GetAllCoupons();
        }

        public async Task<IActionResult> GetCouponById(int id)
        {
            return await _dataBaseLayer.GetCouponById(id);
        }

        public async Task<IActionResult> UpdateCoupon(int id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateCoupon(id, form);
        }

        public async Task<IActionResult> DeleteCoupon(int id)
        {
            return await _dataBaseLayer.DeleteCoupon(id);
        }

        public async Task<IActionResult> ToggleCoupon(int id)
        {
            return await _dataBaseLayer.ToggleCoupon(id);
        }
    }
}
