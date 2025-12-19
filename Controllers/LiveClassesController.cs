using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareerCracker.Controllers
{
    [EnableCors]
    [ApiController]
    [Route("api/admin")]
    [Authorize (Roles = "ADMIN,SUPERADMIN")]
    public class LiveClassesController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public LiveClassesController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
            
        }

        [Route("add-live-class/{batchId}")]
        [HttpPost]
        public async Task<IActionResult> CreateLiveClass(int batchId,IFormCollection form)
        {
            try
            {
                var name = form["topic_name"];

                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new {success = false, message = "Topic name is required!" });
                }
                return await _businessLayer.CreateLiveClass(batchId, form);
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

        [Route("update-live-class/{liveClassId}")]
        [HttpPost]
        public async Task<IActionResult> UpdateLiveClass(int liveClassId, IFormCollection form)
        {
            try
            {
                //var name = form["topic_name"];

                //if (string.IsNullOrEmpty(name))
                //{
                //    return BadRequest(new { success = false, message = "Topic name is required!" });
                //}
                return await _businessLayer.UpdateLiveClass(liveClassId, form);
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

        [Route("update-recording/{liveClassId}")]
        [HttpPut]
        public async Task<IActionResult> UpdateRecordingClass(int liveClassId, IFormCollection form)
        {
            try
            {
                //var name = form["topic_name"];

                //if (string.IsNullOrEmpty(name))
                //{
                //    return BadRequest(new { success = false, message = "Topic name is required!" });
                //}
                return await _businessLayer.UpdateRecordingClass(liveClassId, form);
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

        [Route("get-all-live-class")]
        [HttpGet]
        public async Task<IActionResult> GetAllLiveClasses()
        {
            try
            {
                return await _businessLayer.GetAllLiveClasses();
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

        [Route("delete-live-class/{liveClassId}")]
        [HttpDelete]
        public async Task<IActionResult> HardDeleteLiveClass(int liveClassId)
        {
            try
            {
                return await _businessLayer.HardDeleteLiveClass(liveClassId);
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

        [Route("get-class-attendance/{liveClassId}")]
        [HttpGet]
        public async Task<IActionResult> GetAttendanceByLiveClass(int liveClassId)
        {
            try
            {
                return await _businessLayer.GetAttendanceByLiveClass(liveClassId);
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

        
        [Route("get-by-user-class-attendance/{userId}")]
        [HttpGet]
        public async Task<IActionResult> GetAttendanceByUser(Guid userId)
        {
            try
            {
                return await _businessLayer.GetAttendanceByUser(userId);
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

        [Route("delete-class-attendance/{attendanceId}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteLiveClassAttendance(int attendanceId)
        {
            try
            {
                return await _businessLayer.DeleteLiveClassAttendance(attendanceId);
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
