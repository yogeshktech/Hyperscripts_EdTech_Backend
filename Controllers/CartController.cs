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

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            return await _businessLayer.AddToCart(courseId, userEmail, clientIp);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
        // ✔ Get cart items (GET)
    [AllowAnonymous]
    [Route("get-cart")]
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        try
        {
            string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            return await _businessLayer.GetToCart(userEmail, clientIp);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [AllowAnonymous]
    [Route("del-cart/{cartId}")]
    [HttpDelete]
    public async Task<IActionResult> DeleteCarts(int cartId)
    {
        try
        {
            if(cartId == 0)
            {
                return BadRequest(new { success = false, message = "Bad request!" });
            }

            return await _businessLayer.DeleteCart(cartId);
        }
        catch(Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

}
