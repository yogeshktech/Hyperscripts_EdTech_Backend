using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/order")]
    [Authorize(Roles = "USER,ADMIN,SUPERADMIN")]
    public class OrdersController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public OrdersController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        // 1️⃣ CREATE ORDER
        [Route("create-order")]
        [HttpPost]
        public async Task<IActionResult> CreateOrder(IFormCollection form)
        {
            try
            {
                string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;
                return await _businessLayer.CreateOrder(userEmail,form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 2️⃣ ADD ORDER ITEM
        [Route("add-order-item")]
        [HttpPost]
        public async Task<IActionResult> AddOrderItem(IFormCollection form)
        {
            try
            {
                return await _businessLayer.AddOrderItem(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 3️⃣ GET ORDER BY ID
        [Route("get-order")]
        [HttpGet]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                return await _businessLayer.GetOrder(orderId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 4️⃣ GET ALL ORDERS
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                return await _businessLayer.GetAllOrders();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 5️⃣ UPDATE ORDER STATUS
        [Route("update-status")]
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(IFormCollection form)
        {
            try
            {
                return await _businessLayer.UpdateOrderStatus(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 6️⃣ UPDATE PAYMENT STATUS
        [Route("update-payment")]
        [HttpPost]
        public async Task<IActionResult> UpdatePaymentStatus(IFormCollection form)
        {
            try
            {
                return await _businessLayer.UpdatePaymentStatus(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 7️⃣ DELETE ORDER
        [Route("delete")]
        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                return await _businessLayer.DeleteOrder(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

}
