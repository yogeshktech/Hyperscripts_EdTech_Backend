using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/admin")]
    //[Authorize(Roles = "ADMIN,SUPERADMIN")]
    public class FrontEndController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public FrontEndController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;

        }
        [Route("all-courses")]
        [HttpGet]
        public async Task<IActionResult> GetAllCourse()
        {
            try
            {


                return await _businessLayer.GetAllCourses();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }
    }
}
