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
    [Authorize(Roles = "USER,SUPERADMIN")]
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

        [Route("live-class-attendance/{liveClassId}")]
        [HttpPost]
        public async Task<IActionResult> CreateLiveClassAttendance(int liveClassId)
        {
            try
            {
                string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();
                return await _businessLayer.CreateLiveClassAttendance(liveClassId, userEmail);
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

        [Route("update-class-attendance/{attendanceId}")]
        [HttpPut]
        public async Task<IActionResult> UpdateLiveClassAttendance(int attendanceId)
        {
            try
            {
                return await _businessLayer.UpdateLiveClassAttendance(attendanceId);
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

        [Route("get-recording-class/{batchId}")]
        [HttpGet]
        public async Task<IActionResult> GetRecordingClass(int batchId)
        {
            try
            {

                return await _businessLayer.GetRecordingClass(batchId);
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

        [Route("get-batch-by-userid")]
        [HttpDelete]
        public async Task<IActionResult> GetBatchByUserId()
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                return await _businessLayer.GetBatchByUserId(userEmail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Route("get-all-live-class/{batchId}")]
        [HttpGet]
        public async Task<IActionResult> GetLiveClassesByBatch(int batchId)
        {
            try
            {
                return await _businessLayer.GetLiveClassesByBatch(batchId);
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

        [Route("update-user-detail")]
        [HttpPost]
        public async Task<IActionResult> UpdateUserDetailByUserId(IFormCollection form)
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                return await _businessLayer.UpdateUserDetailByUserId(userEmail,form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
