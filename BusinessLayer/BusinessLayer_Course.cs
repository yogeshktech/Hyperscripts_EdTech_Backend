using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Course
    {
        Task<IActionResult> AddCourse(IFormCollection form);
        Task<IActionResult> UpdateCourse(int id, IFormCollection form);
        Task<IActionResult> GetAllCourses();
        Task<IActionResult> GetCoursesWithFilters(
            int? categoryId,
            List<int>? categoryIds,
            string? categorySlug,
            List<string>? categorySlugs,
            int? languageId,
            List<int>? languageIds,
            string? languageSlug,
            List<string>? languageSlugs,
            decimal? minAverageRating,
            int? minReviewCount,
            string? search,
            int page,
            int pageSize);
        Task<IActionResult> GetCourseById(int id);
        Task<IActionResult> DeleteCourse(int id);
        Task<IActionResult> ToggleCourseStatus(int id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Course { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> AddCourse(IFormCollection form)
        {
            return await _dataBaseLayer.AddCourse(form);
        }
        public async Task<IActionResult> UpdateCourse(int id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateCourse(id,form);
        }
        public async Task<IActionResult> GetAllCourses()
        {
            return await _dataBaseLayer.GetAllCourses();
        }

        public async Task<IActionResult> GetCoursesWithFilters(
            int? categoryId,
            List<int>? categoryIds,
            string? categorySlug,
            List<string>? categorySlugs,
            int? languageId,
            List<int>? languageIds,
            string? languageSlug,
            List<string>? languageSlugs,
            decimal? minAverageRating,
            int? minReviewCount,
            string? search,
            int page,
            int pageSize)
        {
            return await _dataBaseLayer.GetCoursesWithFilters(
                categoryId, categoryIds, categorySlug, categorySlugs, languageId, languageIds, languageSlug, languageSlugs,
                minAverageRating, minReviewCount, search, page, pageSize);
        }

        public async Task<IActionResult> GetCourseById(int id)
        {
            return await _dataBaseLayer.GetCourseById(id);
        }

        public async Task<IActionResult> DeleteCourse(int id)
        {
            return await _dataBaseLayer.DeleteCourse(id);
        }
        public async Task<IActionResult> ToggleCourseStatus(int id)
        {
            return await _dataBaseLayer.ToggleCourseStatus(id);
        }
    }
}
