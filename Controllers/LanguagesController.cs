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
    public class LanguagesController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public LanguagesController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("add-lang")]
        public async Task<IActionResult> InsertLanguage(IFormCollection form)
        {
            try
            {
                var name = form["language_name"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Language name is required"
                    });
                }

                return await _businessLayer.InsertLanguage(form);
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
        [HttpPost("update-lang/{id}")]
        public async Task<IActionResult> UpdateLanguage(int id, IFormCollection form)
        {
            try
            {
                var name = form["language_name"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Language name is required"
                    });
                }

                return await _businessLayer.UpdateLanguage(id, form);
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

        [Route("get-all-lang")]
        [HttpGet]
        public async Task<IActionResult> GetAllLanguages()
        {
            try
            {


                return await _businessLayer.GetAllLanguages();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [HttpGet("get-lang/{id}")]
        public async Task<IActionResult> GetLanguageById(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request!" });
                }

                return await _businessLayer.GetLanguageById(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("del-lang/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteLanguage(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.DeleteLanguage(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

        [Route("status-lang/{id}")]
        [HttpPut]
        public async Task<IActionResult> ToggleLanguageStatus(int id)
        {
            try
            {
                if (id == null && id == 0)
                {
                    return BadRequest(new { success = false, message = "Bad request " });
                }
                return await _businessLayer.ToggleLanguageStatus(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error!{ex.Message}" });
            }
        }

    }
}
