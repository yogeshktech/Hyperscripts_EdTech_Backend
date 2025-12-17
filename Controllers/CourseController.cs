using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN,SUPERADMIN")]
    public class CourseController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public CourseController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("add-course")]
        public async Task<IActionResult> AddCourse(IFormCollection form)
        {
            try
            {
                var name = form["courseName"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Course name is required"
                    });
                }

                return await _businessLayer.AddCourse(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error! {ex.Message}"
                });
            }
        }
        [HttpPost("update-course/{id}")]
        public async Task<IActionResult> UpdateCourse(int id, IFormCollection form)
        {
            try
            {
                var name = form["courseName"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Course name is required"
                    });
                }

                return await _businessLayer.UpdateCourse(id,form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error! {ex.Message}"
                });
            }
        }

        [Route("get-all-course")]
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

        [HttpGet("get-course/{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.GetCourseById(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("del-course/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.DeleteCourse(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("status-course/{id}")]
        [HttpPut]
        public async Task<IActionResult> StatusCourse(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.ToggleCourseStatus(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }
    }
}
