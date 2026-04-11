using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.Controllers
{
    /// <summary>Public course catalog for students (no admin role required).</summary>
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/courses")]
    [AllowAnonymous]
    public class CoursesCatalogController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public CoursesCatalogController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        /// <summary>
        /// Filter and search courses: categoryId or categorySlug, languageId or languageSlug,
        /// minAverageRating and minReviewCount from active reviews, global search on course text, category name, language name.
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> Filter(
            [FromQuery] int? categoryId,
            [FromQuery] string? categorySlug,
            [FromQuery] int? languageId,
            [FromQuery] string? languageSlug,
            [FromQuery] decimal? minAverageRating,
            [FromQuery] int? minReviewCount,
            [FromQuery] string? search,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var term = search ?? q;
                return await _businessLayer.GetCoursesWithFilters(
                    categoryId,
                    categorySlug,
                    languageId,
                    languageSlug,
                    minAverageRating,
                    minReviewCount,
                    term,
                    page,
                    pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}
