using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.Controllers
{
    [ApiController]
    [Route("api/contact")]
    [AllowAnonymous]
    [EnableCors("CorsPolicy")]
    public class EnquiryController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public EnquiryController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("create")]
        public async Task<IActionResult> ContactEnquiry(IFormCollection form)
        {
            return await _businessLayer.ContactEnquiry(form);
        }

        [HttpPost("career")]
        public async Task<IActionResult> Careerjob(IFormCollection form)
        {
            return await _businessLayer.Careerjob(form);
        }
    }
}
