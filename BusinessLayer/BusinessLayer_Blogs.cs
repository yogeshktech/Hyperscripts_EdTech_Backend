using CareerCracker.Models;
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
        Task<IActionResult> BlogAddCommentByEmail(int blogId,string userEmail,AddCommentDto dto);
        Task<IActionResult> GetCommentsByBlogId(int blogId);
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

        public async Task<IActionResult> BlogAddCommentByEmail(
            int blogId,
            string userEmail,
            AddCommentDto dto)
        {
            // 🔐 Validate input
            if (dto == null || string.IsNullOrWhiteSpace(dto.Comment))
                return new BadRequestObjectResult("Comment is required");

            // 🔎 Check blog exists
            bool blogExists = await _dataBaseLayer.BlogExists(blogId);
            if (!blogExists)
                return new NotFoundObjectResult("Blog not found");

            // 🔎 Get UserId from Email (AspNetUsers)
            string? userId = await _dataBaseLayer.GetUserIdByEmail(userEmail);
            if (string.IsNullOrEmpty(userId))
                return new UnauthorizedObjectResult("User not found");

            // 💾 Save comment
            return await _dataBaseLayer.BlogAddComment(blogId, userId, dto);
        }

        public async Task<IActionResult> GetCommentsByBlogId(int blogId)
        {
            return await _dataBaseLayer.GetCommentsByBlogId(blogId);
        }
    }

    }

