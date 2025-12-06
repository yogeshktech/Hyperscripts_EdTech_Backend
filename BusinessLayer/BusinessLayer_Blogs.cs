using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Blogs
    {
        Task<IActionResult> AddBlogs(IFormCollection form);
        Task<IActionResult> UpdateBlogs(int id, IFormCollection form);
        Task<IActionResult> GetAllBlogs();
        Task<IActionResult> GetBlogById(int id);
        Task<IActionResult> DeleteBlog(int id);
        Task<IActionResult> ToggleBlogStatus(int id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Blogs
    {

    }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> AddBlogs(IFormCollection form)
        {
            return await _dataBaseLayer.AddBlogs(form);
        }

        public async Task<IActionResult> UpdateBlogs(int id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateBlogs(id, form);
        }
        public async Task<IActionResult> GetAllBlogs()
        {
            return await _dataBaseLayer.GetAllBlogs();
        }

        public async Task<IActionResult> GetBlogById(int id)
        {
            return await _dataBaseLayer.GetBlogById(id);
        }

        public async Task<IActionResult> DeleteBlog(int id)
        {
            return await _dataBaseLayer.DeleteBlog(id);
        }
        public async Task<IActionResult> ToggleBlogStatus(int id)
        {
            return await _dataBaseLayer.ToggleBlogStatus(id);
        }
    }
}
