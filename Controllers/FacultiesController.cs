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
            if (string.IsNullOrWhiteSpace(form["FirstName"]))
                return BadRequest(new { success = false, message = "First name is required" });

            // Optional: Last name validation
            // if (string.IsNullOrWhiteSpace(form["LastName"]))
            //     return BadRequest(new { success = false, message = "Last name is required" });

            return await _businessLayer.InsertFaculty(form);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    [Route("update-faculty/{id}")]
    [HttpPut]
    public async Task<IActionResult> UpdateFaculty(string id, IFormCollection form)
    {
        return await _businessLayer.UpdateFaculty(id, form);
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
