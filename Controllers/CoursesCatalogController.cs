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
            [FromQuery] string? categoryIds,
            [FromQuery] string? categorySlug,
            [FromQuery] string? categorySlugs,
            [FromQuery] int? languageId,
            [FromQuery] string? languageIds,
            [FromQuery] string? languageSlug,
            [FromQuery] string? languageSlugs,
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
                var parsedCategoryIds = ParseCsvInts(categoryIds);
                var parsedLanguageIds = ParseCsvInts(languageIds);
                var parsedCategorySlugs = ParseCsvStrings(categorySlugs);
                var parsedLanguageSlugs = ParseCsvStrings(languageSlugs);

                return await _businessLayer.GetCoursesWithFilters(
                    categoryId,
                    parsedCategoryIds,
                    categorySlug,
                    parsedCategorySlugs,
                    languageId,
                    parsedLanguageIds,
                    languageSlug,
                    parsedLanguageSlugs,
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

        private static List<int>? ParseCsvInts(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return null;
            var list = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => int.TryParse(v, out var n) ? (int?)n : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .Distinct()
                .ToList();
            return list.Count == 0 ? null : list;
        }

        private static List<string>? ParseCsvStrings(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return null;
            var list = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return list.Count == 0 ? null : list;
        }
    }
}
