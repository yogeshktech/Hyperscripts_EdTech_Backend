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
    public class BatchController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public BatchController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
            
        }
        [Route("new-batch")]
        [HttpPost]
        public async Task<IActionResult> NewBatch(IFormCollection form)
        {
            try
            {
                var name = form["batch_name"];
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new {success = false, message = "Batch Name is required!"});
                }
                //string userEmail =
                //    User.FindFirst(ClaimTypes.Email)?.Value ??
                //    User.FindFirst("email")?.Value ??
                //    User.FindFirst("UserEmail")?.Value;

                //if (string.IsNullOrEmpty(userEmail))
                //    return Unauthorized();

                return await _businessLayer.NewBatch(form);
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

        [Route("get-batch")]
        [HttpGet]
        public async Task<IActionResult> GetAllBatchs()
        {
            try
            {
                return await _businessLayer.GetAllBatchs();
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

        [Route("get-batch/{courseId}")]
        [HttpGet]
        public async Task<IActionResult> GetByIdBatchs(int courseId)
        {
            try
            {
                return await _businessLayer.GetByIdBatchs(courseId);
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

        [Route("update-batch/{batchId}")]
        [HttpPost]
        public async Task<IActionResult> UpdateBatch(int batchId,IFormCollection form)
        {
            try
            {
                //var name = form["batch_name"];
                //if (string.IsNullOrWhiteSpace(name))
                //{
                //    return BadRequest(new { success = false, message = "Batch Name is required!" });
                //}
                //string userEmail =
                //    User.FindFirst(ClaimTypes.Email)?.Value ??
                //    User.FindFirst("email")?.Value ??
                //    User.FindFirst("UserEmail")?.Value;

                //if (string.IsNullOrEmpty(userEmail))
                //    return Unauthorized();

                return await _businessLayer.UpdateBatch(batchId,form);
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
