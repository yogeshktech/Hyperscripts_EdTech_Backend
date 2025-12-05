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
    public class CategoryController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public CategoryController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [Route("insert-category")]
        [HttpPost]
        public async Task<IActionResult> InserCategory(IFormCollection form)
        {
            try
            {
                var name = form["CategoryName"];

                if (string.IsNullOrEmpty(name)) 
                {
                    return BadRequest(new { success = false, message = $"Category name is required" });
                }
                return await _businessLayer.InserCategory(form);
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new {success = false, message = $"Internal server error!{ex.Message}"});
            }
        
        }

        [Route("update-category/{Id}")]
        [HttpPost]
        public async Task<IActionResult> UpdataCategory(int Id, IFormCollection form)
        {
            try
            {
                if (Id == null && Id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.UpdataCategory(Id, form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("get-categories")]
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                

                return await _businessLayer.GetAllCategory();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [HttpGet("get-categories/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.GetCategoryById(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }


        [Route("del-categories/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.DeleteCategoryById(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [HttpPut("category-status/{id}")]
        public async Task<IActionResult> Toggle(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.ToggleCategoryStatus(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

    }
}
