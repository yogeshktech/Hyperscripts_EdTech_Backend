using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Course
    {
        Task<IActionResult> AddCourse(IFormCollection form);
        Task<IActionResult> UpdateCourse(int id, IFormCollection form);
        Task<IActionResult> GetAllCourses();
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
