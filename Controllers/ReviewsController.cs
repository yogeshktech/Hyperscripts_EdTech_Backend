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
    [Authorize(Roles = "USER,ADMIN,SUPERADMIN")]
    public class ReviewsController : Controller
    {
       
            private readonly IBusinessLayer _businessLayer;
            public ReviewsController(IBusinessLayer businessLayer)
            {
                _businessLayer = businessLayer;

            }

            [HttpPost("add-review")]
            public async Task<IActionResult> AddReview(IFormCollection form)
            {
                try
                {
                string userEmail =
           User.FindFirst(ClaimTypes.Email)?.Value ??
           User.FindFirst("email")?.Value ??
           User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {

                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }
                var name = form["title"];

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Name is required"
                        });
                    }

                    //return Ok(form);

                    return await _businessLayer.AddReview(userEmail,form);
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

        [HttpPost("add-review-by-admin")]
        public async Task<IActionResult> AddReviewByAdmin(IFormCollection form)
        {
            try
            {
           //     string userEmail =
           //User.FindFirst(ClaimTypes.Email)?.Value ??
           //User.FindFirst("email")?.Value ??
           //User.FindFirst("UserEmail")?.Value;

           //     if (string.IsNullOrEmpty(userEmail))
           //     {

           //         return Unauthorized(new { success = false, message = "User not authenticated" });
           //     }
                var name = form["title"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Name is required"
                    });
                }

                //return Ok(form);

                return await _businessLayer.AddReviewByAdmin( form);
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

        [HttpPost("update-review/{id}")]
            public async Task<IActionResult> UpdateReview(int id, IFormCollection form)
            {
                try
                {
                string userEmail =
           User.FindFirst(ClaimTypes.Email)?.Value ??
           User.FindFirst("email")?.Value ??
           User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {

                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }
                var name = form["title"];

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Name is required"
                        });
                    }

                    return await _businessLayer.UpdateReview(id, userEmail, form);
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

        [HttpPost("update-review-by-admin/{id}")]
        public async Task<IActionResult> UpdateReviewByAdmin(int id, IFormCollection form)
        {
            try
            {
           //     string userEmail =
           //User.FindFirst(ClaimTypes.Email)?.Value ??
           //User.FindFirst("email")?.Value ??
           //User.FindFirst("UserEmail")?.Value;

           //     if (string.IsNullOrEmpty(userEmail))
           //     {

           //         return Unauthorized(new { success = false, message = "User not authenticated" });
           //     }
                var name = form["title"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Name is required"
                    });
                }

                return await _businessLayer.UpdateReviewByAdmin(id, form);
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


        [Route("get-all-review/{courseId}")]
            [HttpGet]
            public async Task<IActionResult> GetReviews(int courseId)
            {
                try
                {


                    return await _businessLayer.GetReviews(courseId);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
                }
            }

            //[HttpGet("get-review/{id}")]
            //public async Task<IActionResult> GetTestimonialById(int id)
            //{
            //    try
            //    {
            //        if (id == null && id == 0)
            //        {
            //            return BadRequest(new { success = false, message = "Bad request!" });
            //        }

            //        return await _businessLayer.GetTestimonialById(id);
            //    }
            //    catch (Exception ex)
            //    {
            //        return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            //    }
            //}

            [Route("del-review/{id}")]
            [HttpDelete]
            public async Task<IActionResult> DeleteReview(int id)
            {
                try
                {
                    if (id == null && id == 0)
                    {
                        return BadRequest(new { success = false, message = "Bad request " });
                    }
                    return await _businessLayer.DeleteReview(id);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
                }
            }

            [Route("status-review/{id}")]
            [HttpPut]
            public async Task<IActionResult> ToggleReview(int id)
            {
                try
                {
                    if (id == null && id == 0)
                    {
                        return BadRequest(new { success = false, message = "Bad request " });
                    }
                    return await _businessLayer.ToggleReview(id);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
                }
            }

        [Route("get-all-review-admin")]
        [HttpGet]
        public async Task<IActionResult> getAllReviewByAdmin()
        {
            return await _businessLayer.getAllReviewByAdmin();
            //return Ok(new {Success = true,Message = "Review list successfully", data= data});
        }
        }
    }

