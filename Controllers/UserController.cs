using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/user")]
    [Authorize(Roles = "USER")]
    public class UserController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public UserController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
            
        }

        [Route("my-course")]
        [HttpGet]
        public async Task<IActionResult> MyCourses()
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                return await _businessLayer.MyCourses(userEmail);
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

        [Route("my-batch/{courseId}")]
        [HttpGet]
        public async Task<IActionResult> MyBatch(int courseId)
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                return await _businessLayer.MyBatch(courseId,userEmail);
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
