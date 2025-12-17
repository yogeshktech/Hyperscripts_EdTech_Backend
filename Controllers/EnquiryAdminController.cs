using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

[EnableCors("CorsPolicy")]
[ApiController]
[Route("api/admin/enquiry")]
[Authorize(Roles = "ADMIN,SUPERADMIN")]
public class EnquiryAdminController : ControllerBase
{
    private readonly IBusinessLayer _businessLayer;

    public EnquiryAdminController(IBusinessLayer businessLayer)
    {
        _businessLayer = businessLayer;
    }

    // GET ALL
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        return await _businessLayer.GetAllEnquiries();
    }

    // GET BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return await _businessLayer.GetEnquiryById(id);
    }

    // DELETE
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await _businessLayer.DeleteEnquiry(id);
    }

    // Career Job 
    //[HttpGet("all-career")]
    //public async Task<IActionResult> AllResume()
    //{
    //    return await _businessLayer.GetAllResume();
    //}

    //[HttpGet("career/{id}")]
    //public async Task<IActionResult> GetResumeById(int id)
    //{
    //    return await _businessLayer.GetCareerById(id);
    //}

    //[HttpDelete("career/delete/{id}")]
    //public async Task<IActionResult> DeleteResume(int id)
    //{
    //    return await _businessLayer.DeleteCareer(id);
    //}

    [HttpGet("all-career")]
    public async Task<IActionResult> AllResume()
    {
        return await _businessLayer.GetAllResume();
    }

    [HttpGet("career/{id}")]
    public async Task<IActionResult> GetResumeById(int id)
    {
        return await _businessLayer.GetCareerById(id);
    }

    [HttpDelete("career/delete/{id}")]
    public async Task<IActionResult> DeleteResume(int id)
    {
        return await _businessLayer.DeleteCareer(id);
    }
}
