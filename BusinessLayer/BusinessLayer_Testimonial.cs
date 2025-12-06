using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Testimonial
    {
        Task<IActionResult> AddTestimonial(IFormCollection form);
        Task<IActionResult> GetAllTestimonials();
        Task<IActionResult> GetTestimonialById(int id);
        Task<IActionResult> UpdateTestimonial(int id, IFormCollection form);
        Task<IActionResult> DeleteTestimonial(int id);
        Task<IActionResult> ToggleStatus(int id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Testimonial { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> AddTestimonial(IFormCollection form)
        {
            return await _dataBaseLayer.AddTestimonial(form);
        }
        public async Task<IActionResult> GetAllTestimonials()
        {
            return await _dataBaseLayer.GetAllTestimonials();
        }
        public async Task<IActionResult> GetTestimonialById(int id)
        {
            return await _dataBaseLayer.GetTestimonialById(id);
        }
        public async Task<IActionResult> UpdateTestimonial(int id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateTestimonial(id, form);
        }
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            return await _dataBaseLayer.DeleteTestimonial(id);
        }
        public async Task<IActionResult> ToggleStatus(int id)
        {
            return await _dataBaseLayer.ToggleStatus(id);
        }
    }
}
