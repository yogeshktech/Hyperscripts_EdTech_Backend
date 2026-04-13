using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "SUPERADMIN,ADMIN")]
    public class AdminOrderController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public AdminOrderController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;

        }



        [HttpGet("get-admin-orders")]
        public async Task<IActionResult> GetAllAdminOrders()
        {
            try
            {
                var result = await _businessLayer.GetAllAdminOrders();

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
                });
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

        [HttpGet("dashboard-report")]
        public async Task<IActionResult> DashboardReport()
        {
            try
            {
                return await _businessLayer.GetDashboardReport();
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
