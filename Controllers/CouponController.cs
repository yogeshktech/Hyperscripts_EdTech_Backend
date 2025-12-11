using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/coupon")]
    [Authorize (Roles = "ADMIN, SUPERADMIN, USER")]
    public class CouponController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public CouponController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
            
        }

        [Route("add-coupon")]
        [HttpPost]
        public async Task<IActionResult> AddCoupon(IFormCollection form)
        {
            try
            {
                var name = form["couponName"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Course name is required"
                    });
                }

                return await _businessLayer.AddCoupon(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =  ex.Message
                });
            }
        }

        [Route("get-all-coupon")]
        [HttpGet]
        public async Task<IActionResult> GetAllCoupons()
        {
            try
            {
                //var name = form["couponName"];

                //if (string.IsNullOrWhiteSpace(name))
                //{
                //    return BadRequest(new
                //    {
                //        success = false,
                //        message = "Course name is required"
                //    });
                //}

                return await _businessLayer.GetAllCoupons();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("get-coupon/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetCouponById(int id)
        {
            try
            {
                //var name = form["couponName"];

                //if (string.IsNullOrWhiteSpace(name))
                //{
                //    return BadRequest(new
                //    {
                //        success = false,
                //        message = "Course name is required"
                //    });
                //}

                return await _businessLayer.GetCouponById(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("update-coupon/{id}")]
        [HttpPost]
        public async Task<IActionResult> UpdateCoupon(int id, IFormCollection form)
        {
            try
            {
                var name = form["couponName"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Course name is required"
                    });
                }

                return await _businessLayer.UpdateCoupon(id,form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }

        }

        [Route("delete-coupon/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            try
            {
                //var name = form["couponName"];

                //if (string.IsNullOrWhiteSpace(name))
                //{
                //    return BadRequest(new
                //    {
                //        success = false,
                //        message = "Course name is required"
                //    });
                //}

                return await _businessLayer.DeleteCoupon(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("status-coupon/{id}")]
        [HttpPost]
        public async Task<IActionResult> ToggleCoupon(int id)
        {
            try
            {
                //var name = form["couponName"];

                //if (string.IsNullOrWhiteSpace(name))
                //{
                //    return BadRequest(new
                //    {
                //        success = false,
                //        message = "Course name is required"
                //    });
                //}

                return await _businessLayer.ToggleCoupon(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("apply-coupon")]
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(IFormCollection form)
        {
            try
            {
                var couponCode = form["couponName"];

                if (string.IsNullOrWhiteSpace(couponCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Coupon name is required"
                    });
                }

                return await _businessLayer.ApplyCoupon(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
