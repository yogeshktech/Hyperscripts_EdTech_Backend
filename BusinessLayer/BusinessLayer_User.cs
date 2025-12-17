using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_User
    {
        Task<IActionResult> MyCourses(string userEmail);
    }

    public partial interface IBusinessLayer : IBusinessLayer_User { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> MyCourses(string userEmail)
        {
            return await _dataBaseLayer.MyCourses(userEmail);
        }
    }
}
