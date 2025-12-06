using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Languages
    {
        Task<IActionResult> InsertLanguage(IFormCollection form);
        Task<IActionResult> ToggleLanguageStatus(int id);
        Task<IActionResult> DeleteLanguage(int id);
        Task<IActionResult> UpdateLanguage(int id, IFormCollection form);
        Task<IActionResult> GetLanguageById(int id);
        Task<IActionResult> GetAllLanguages();
    }

    public partial interface IBusinessLayer : IBusinessLayer_Languages { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> InsertLanguage(IFormCollection form)
        {
            return await _dataBaseLayer.InsertLanguage(form);
        }
        public async Task<IActionResult> ToggleLanguageStatus(int id)
        {
            return await _dataBaseLayer.ToggleLanguageStatus(id);
        }
        public async Task<IActionResult> DeleteLanguage(int id)
        {
            return await _dataBaseLayer.DeleteLanguage(id);
        }
        public async Task<IActionResult> UpdateLanguage(int id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateLanguage(id, form);
        }
        public async Task<IActionResult> GetLanguageById(int id)
        {
            return await _dataBaseLayer.GetLanguageById(id);
        }
        public async Task<IActionResult> GetAllLanguages()
        {
            return await _dataBaseLayer.GetAllLanguages();
        }
    }
}
