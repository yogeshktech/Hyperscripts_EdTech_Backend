using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Reviews
    {
        Task<IActionResult> AddReview(string userEmail, IFormCollection form);
        Task<IActionResult> AddReviewByAdmin(IFormCollection form);
        Task<IActionResult> UpdateReview(int id, string userEmail, IFormCollection form);
        Task<IActionResult> UpdateReviewByAdmin(int id, IFormCollection form);
        Task<IActionResult> GetReviews(int courseId);
        Task<IActionResult> DeleteReview(int id);
        Task<IActionResult> ToggleReview(int id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Reviews { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> AddReview(string userEmail, IFormCollection form)
        {
            return await _dataBaseLayer.AddReview(userEmail,form);
        }

        public async Task<IActionResult> AddReviewByAdmin( IFormCollection form)
        {
            return await _dataBaseLayer.AddReviewByAdmin(form);
        }

        public async Task<IActionResult> UpdateReview(int id, string userEmail, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateReview(id, userEmail, form);
        }
        public async Task<IActionResult> UpdateReviewByAdmin(int id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateReviewByAdmin(id, form);
        }
        public async Task<IActionResult> GetReviews(int courseId)
        {
            return await _dataBaseLayer.GetReviews(courseId);
        }

        public async Task<IActionResult> DeleteReview(int id)
        {
            return await _dataBaseLayer.DeleteReview(id);
        }

        public async Task<IActionResult> ToggleReview(int id)
        {
            return await _dataBaseLayer.ToggleReview(id);
        }
    }
}
