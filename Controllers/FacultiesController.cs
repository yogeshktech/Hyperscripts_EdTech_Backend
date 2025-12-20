using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    [Route("asign-batch-fac/{batchId}")]
    [HttpPost]
    public async Task<IActionResult> AsignBatch(int batchId, IFormCollection form)
    {
        try
        {
            var userEmail = form["email"];
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return BadRequest(new { success = false, message = "Faculties Email is required!" });
            }

            return await _businessLayer.AsignBatch(batchId, form);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new {success = false, message = ex.Message });
        }
    }

    [Route("update-batch-fac/{assignId}")]
    [HttpPut]
    public async Task<IActionResult> UpdateAssignedFaculty(int assignId, IFormCollection form)
    {
        try
        {
            var userEmail = form["email"];
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return BadRequest(new { success = false, message = "Faculties Email is required!" });
            }

            return await _businessLayer.UpdateAssignedFaculty(assignId, form);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [Route("get-batch-facId")]
    [HttpGet]
    public async Task<IActionResult> GetAssignedFaculty(IFormCollection form)
    {
        try
        {
            var userEmail = form["email"];
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return BadRequest(new { success = false, message = "Faculties Email is required!" });
            }

            return await _businessLayer.GetAssignedFaculty( form);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [Route("get-batch-facultyId")]
    [HttpGet]
    public async Task<IActionResult> GetAssignedFacultyEmail()
    {
        try
        {
            string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            return await _businessLayer.GetAssignedFacultyEmail(userEmail);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [Route("delete-assign-batch/{facultyAssignId}")]
    [HttpDelete]
    public async Task<IActionResult> DeleteAssignedFaculty (int facultyAssignId)
    {
        try
        {

            return await _businessLayer.DeleteAssignedFaculty(facultyAssignId);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [Route("soft-delete-assign-batch/{facultyAssignId}")]
    [HttpDelete]
    public async Task<IActionResult> SoftDeleteAssignedFaculty(int facultyAssignId)
    {
        try
        {

            return await _businessLayer.SoftDeleteAssignedFaculty(facultyAssignId);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
