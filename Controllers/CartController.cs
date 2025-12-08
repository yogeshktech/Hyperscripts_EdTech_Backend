using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[EnableCors("CorsPolicy")]
[ApiController]
[Route("api/cart")]
[Authorize(Roles = "ADMIN,SUPERADMIN,USER")] // default for other APIs
public class CartController : ControllerBase
{
    private readonly IBusinessLayer _businessLayer;
    public CartController(IBusinessLayer businessLayer)
    {
        _businessLayer = businessLayer;
    }

    // ✔ Allow guest users for add-to-cart
    [AllowAnonymous]
    [Route("add-cart/{courseId}")]
    [HttpPost]
    public async Task<IActionResult> AddToCart(int courseId)
    {
        try
        {
            string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

            // ✔ Get visitor IP
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            return await _businessLayer.AddToCart(courseId, userEmail, clientIp);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
