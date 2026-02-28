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

    public class TestimonialController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public TestimonialController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
            
        }

        [HttpPost("add-testimonial")]
        public async Task<IActionResult> AddTestimonial(IFormCollection form)
        {
            try
            {
                var name = form["test_name"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Name is required"
                    });
                }

                //return Ok(form);

                return await _businessLayer.AddTestimonial(form);
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

        [HttpPost("update-testimonial/{id}")]
        public async Task<IActionResult> UpdateTestimonial(int id, IFormCollection form)
        {
            try
            {
                var name = form["test_name"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Name is required"
                    });
                }

                return await _businessLayer.UpdateTestimonial(id, form);
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

        [Route("get-all-testimonial")]
        [HttpGet]
        public async Task<IActionResult> GetAllTestimonials()
        {
            try
            {


                return await _businessLayer.GetAllTestimonials();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [HttpGet("get-testimonial/{id}")]
        public async Task<IActionResult> GetTestimonialById(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.GetTestimonialById(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("del-testimonial/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.DeleteTestimonial(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("status-testimonial/{id}")]
        [HttpPut]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.ToggleStatus(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }
    }
}
