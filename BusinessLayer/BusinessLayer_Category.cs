using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Category
    {
        Task<IActionResult> InserCategory(IFormCollection form);
        Task<IActionResult> UpdataCategory(int Id, IFormCollection form);
        Task<IActionResult> GetAllCategory();
        Task<IActionResult> GetCategoryById(int Id);
        Task<IActionResult> DeleteCategoryById(int Id);
        Task<IActionResult> ToggleCategoryStatus(int Id);

    }
    public partial interface IBusinessLayer : IBusinessLayer_Category 
    { 
    }
    public partial class BusinessLayer
    {
        public async Task<IActionResult> InserCategory(IFormCollection form)
        {
            return await _dataBaseLayer.InserCategory(form);
        }

        public async Task<IActionResult> UpdataCategory(int Id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdataCategory(Id, form);
        }

        public async Task<IActionResult> GetAllCategory()
        {
            return await _dataBaseLayer.GetAllCategory();
        }
        public async Task<IActionResult> GetCategoryById(int Id)
        {
            return await _dataBaseLayer.GetCategoryById(Id);
        }

        public async Task<IActionResult> DeleteCategoryById(int Id)
        {
            return await _dataBaseLayer.DeleteCategoryById(Id);
        }

        public async Task<IActionResult> ToggleCategoryStatus(int Id)
        {
            return await _dataBaseLayer.ToggleCategoryStatus(Id);
        }
    }

    
}
