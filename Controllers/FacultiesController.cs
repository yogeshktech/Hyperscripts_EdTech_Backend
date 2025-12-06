using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

[EnableCors("CorsPolicy")]
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN,SUPERADMIN")]
public class FacultiesController : ControllerBase
{
    private readonly IBusinessLayer _businessLayer;

    public FacultiesController(IBusinessLayer businessLayer)
    {
        _businessLayer = businessLayer;
    }

    [Route("add-faculty")]
    [HttpPost]
    public async Task<IActionResult> InsertFaculty(IFormCollection form)
    {
        try
        {
            if (string.IsNullOrEmpty(form["name"]))
                return BadRequest(new { success = false, message = "Faculty name is required" });

            return await _businessLayer.InsertFaculty(form);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ✅ UPDATE using slug
    [Route("update-faculty/{slug}")]
    [HttpPut]
    public async Task<IActionResult> UpdateFaculty(string slug, IFormCollection form)
    {
        return await _businessLayer.UpdateFacultyBySlug(slug, form);
    }

    [Route("faculties")]
    [HttpGet]
    public async Task<IActionResult> GetAllFaculties()
    {
        return await _businessLayer.GetAllFaculties();
    }

    // ✅ GET using slug
    [Route("faculty/{slug}")]
    [HttpGet]
    public async Task<IActionResult> GetFacultyBySlug(string slug)
    {
        return await _businessLayer.GetFacultyBySlug(slug);
    }

    // ✅ DELETE using slug
    [Route("delete-faculty/{slug}")]
    [HttpDelete]
    public async Task<IActionResult> DeleteFaculty(string slug)
    {
        return await _businessLayer.DeleteFacultyBySlug(slug);
    }

    // ✅ TOGGLE STATUS using slug
    [Route("toggle-faculty-status/{slug}")]
    [HttpPatch]
    public async Task<IActionResult> ToggleFacultyStatus(string slug)
    {
        return await _businessLayer.ToggleFacultyStatusBySlug(slug);
    }
}
