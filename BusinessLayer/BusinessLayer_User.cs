using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_User
    {
        Task<IActionResult> GetUserDashboard(string userEmail);
        Task<IActionResult> MyCourses(string userEmail);
        Task<IActionResult> MyBatch(int courseId,string userEmail);
        Task<IActionResult> DeleteUser(IFormCollection form);
        Task<IActionResult> UpdateUser(Guid userId, IFormCollection form);
        Task<IActionResult> UpdateUserDetailByUserId(string userEmail,IFormCollection form);
    }

    public partial interface IBusinessLayer : IBusinessLayer_User { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> GetUserDashboard(string userEmail)
        {
            return await _dataBaseLayer.GetUserDashboard(userEmail);
        }

        public async Task<IActionResult> MyCourses(string userEmail)
        {
            return await _dataBaseLayer.MyCourses(userEmail);
        }
        public async Task<IActionResult> MyBatch(int courseId,string userEmail)
        {
            return await _dataBaseLayer.MyBatch(courseId, userEmail);
        }
        public async Task<IActionResult> DeleteUser(IFormCollection form)
        {
            return await _dataBaseLayer.DeleteUser(form);
        }

        public async Task<IActionResult> UpdateUser(Guid userId, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateUser(userId, form);
        }

        public async Task<IActionResult> UpdateUserDetailByUserId(string userEmail,IFormCollection form)
        {
            return await _dataBaseLayer.UpdateUserDetailByUserId(userEmail,form);
        }
    }
}
