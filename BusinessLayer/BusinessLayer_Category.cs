using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Category
    {
        Task<IActionResult> InserCategory(IFormCollection form);
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
    }

    
}
