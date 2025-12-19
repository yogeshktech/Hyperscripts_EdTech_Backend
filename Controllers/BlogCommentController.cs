using CareerCracker.BusinessLayer;
using CareerCracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareerCracker.Controllers
{
    [ApiController]
    [Route("api/blog-comment")]
    [Authorize(Roles = "USER")]
    public class BlogCommentController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public BlogCommentController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("add/{blogId}")]
        public async Task<IActionResult> AddComment(
            int blogId,
            [FromBody] AddCommentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Comment))
                return BadRequest("Comment is required");

            // 🔥 SAME LOGIC AS UserController
            string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Invalid token");

            // 🔥 Email → UserId handled in business layer
            return await _businessLayer.BlogAddCommentByEmail(blogId, userEmail, dto);
        }

        [HttpGet("blog/{blogId}")]
        [AllowAnonymous] // anyone can view comments
        public async Task<IActionResult> GetComments(int blogId)
        {
            return await _businessLayer.GetCommentsByBlogId(blogId);
        }
    }
}
