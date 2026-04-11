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

        //[HttpPost("add-blogs")]
        //public async Task<IActionResult> AddBlogs(IFormCollection form)
        //{
        //    try
        //    {
        //        var name = form["blogName"];

        //        if (string.IsNullOrWhiteSpace(name))
        //        {
        //            return BadRequest(new
        //            {
        //                success = false,
        //                message = "blog name is required"
        //            });
        //        }

        //        //return Ok(form);

        //        return await _businessLayer.AddBlogs(form);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = $"Internal server error! {ex.Message}"
        //        });
        //    }
        //}



        [HttpPost("add-blogs")]
        public async Task<IActionResult> AddBlogs(IFormCollection form)
        {
            try
            {
                // Safe extraction from IFormCollection
                string name = form["blogName"].ToString().Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Blog name is required"
                    });
                }

                // Optional: You can add more validation here
                string description = form["blogDescription"].ToString().Trim();

                // Delegate the heavy logic to Business Layer
                return await _businessLayer.AddBlogs(form);
            }
            catch (Exception ex)
            {
                // Better logging (if you have ILogger injected)
                // _logger.LogError(ex, "Error in AddBlogs controller");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error while adding blog",
                    // error = ex.Message   // Uncomment only for development
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

        [AllowAnonymous]
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

        [AllowAnonymous]
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
