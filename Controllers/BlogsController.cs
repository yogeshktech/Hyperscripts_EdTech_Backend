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
    public class BlogsController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public BlogsController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("add-blogs")]
        public async Task<IActionResult> AddBlogs(IFormCollection form)
        {
            try
            {
                var name = form["blogName"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "blog name is required"
                    });
                }

                //return Ok(form);

                return await _businessLayer.AddBlogs(form);
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
        [HttpPost("update-blog/{id}")]
        public async Task<IActionResult> UpdateBlogs(int id, IFormCollection form)
        {
            try
            {
                var name = form["blogName"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Name is required"
                    });
                }

                return await _businessLayer.UpdateBlogs(id, form);
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

        [Route("get-all-blogs")]
        [HttpGet]
        public async Task<IActionResult> GetAllBlogs()
        {
            try
            {


                return await _businessLayer.GetAllBlogs();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [HttpGet("get-blog/{id}")]
        public async Task<IActionResult> GetBlogById(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.GetBlogById(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("del-blog/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.DeleteBlog(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("status-blog/{id}")]
        [HttpPut]
        public async Task<IActionResult> ToggleBlogStatus(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.ToggleBlogStatus(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }
    }
}
