using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Batch
    {
        Task<IActionResult> NewBatch(IFormCollection form);
        Task<IActionResult> GetAllBatchs();
        Task<IActionResult> GetByIdBatchs(int courseId);
        Task<IActionResult> UpdateBatch(int batchId ,IFormCollection form);
        Task<IActionResult> DeleteBatchs(int batchId);
        Task<IActionResult> GetBatchByUserId(string userEmail);
        Task<IActionResult> StatusBatchs(int batchId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Batch { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> NewBatch(IFormCollection form)
        {
            return await _dataBaseLayer.NewBatch(form);
        }
        public async Task<IActionResult> GetAllBatchs()
        {
            return await _dataBaseLayer.GetAllBatchs();
        }

        public async Task<IActionResult> GetByIdBatchs(int courseId)
        {
            return await _dataBaseLayer.GetByIdBatchs(courseId);
        }

        public async Task<IActionResult> UpdateBatch(int batchId, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateBatch(batchId, form);
        }
        public async Task<IActionResult> DeleteBatchs(int batchId)
        {
            return await _dataBaseLayer.DeleteBatchs(batchId);
        }

        public async Task<IActionResult> GetBatchByUserId(string userEmail)
        {
            return await _dataBaseLayer.GetBatchByUserId(userEmail);
        }

        public async Task<IActionResult> StatusBatchs(int batchId)
        {
            return await _dataBaseLayer.StatusBatchs(batchId);
        }
    }
}
