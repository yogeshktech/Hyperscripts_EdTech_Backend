using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{


    public interface IBusinessLayer_Faculty
    {
        Task<IActionResult> InsertFaculty(IFormCollection form);
        //Task<IActionResult> UpdateFacultyBySlug(string slug, IFormCollection form);
        Task<IActionResult> UpdateFaculty(string id, IFormCollection form);
        Task<IActionResult> GetAllFaculties();
        Task<IActionResult> GetFacultyBySlug(string slug);
        Task<IActionResult> DeleteFacultyBySlug(string slug);
        Task<IActionResult> ToggleFacultyStatusBySlug(string slug);
        Task<IActionResult> AsignBatch(int batchId, IFormCollection form);
        Task<IActionResult> UpdateAssignedFaculty(int assignId, IFormCollection form);
        Task<IActionResult> GetAssignedFaculty(IFormCollection form);
        Task<IActionResult> GetAssignedFacultyEmail(string userEmail);
        Task<IActionResult> DeleteAssignedFaculty(int facultyAssignId);
        Task<IActionResult> SoftDeleteAssignedFaculty(int facultyAssignId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Faculty
    {
    }

    public partial class BusinessLayer
    {
        // Insert
        public async Task<IActionResult> InsertFaculty(IFormCollection form)
        {
            return await _dataBaseLayer.InsertFaculty(form);
        }

        // ✅ Update by slug
        public async Task<IActionResult> UpdateFaculty(string id, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateFaculty(id, form);
        }

        // Get All
        public async Task<IActionResult> GetAllFaculties()
        {
            return await _dataBaseLayer.GetAllFaculties();
        }

        // ✅ Get by slug
        public async Task<IActionResult> GetFacultyBySlug(string slug)
        {
            return await _dataBaseLayer.GetFacultyBySlug(slug);
        }

        // ✅ Delete by slug
        public async Task<IActionResult> DeleteFacultyBySlug(string slug)
        {
            return await _dataBaseLayer.DeleteFacultyBySlug(slug);
        }

        // ✅ Toggle status by slug
        public async Task<IActionResult> ToggleFacultyStatusBySlug(string slug)
        {
            return await _dataBaseLayer.ToggleFacultyStatusBySlug(slug);
        }

        public async Task<IActionResult> AsignBatch(int batchId, IFormCollection form)
        {
            return await _dataBaseLayer.AsignBatch(batchId, form);  
        }

        public async Task<IActionResult> UpdateAssignedFaculty(int assignId, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateAssignedFaculty(assignId, form);
        }

        public async Task<IActionResult> GetAssignedFaculty(IFormCollection form)
        {
            return await _dataBaseLayer.GetAssignedFaculty(form);
        }

        public async Task<IActionResult> GetAssignedFacultyEmail(string userEmail)
        {
            return await _dataBaseLayer.GetAssignedFacultyEmail(userEmail);
        }
        public async Task<IActionResult> DeleteAssignedFaculty(int facultyAssignId)
        {
            return await _dataBaseLayer.DeleteAssignedFaculty(facultyAssignId);
        }

        public async Task<IActionResult> SoftDeleteAssignedFaculty(int facultyAssignId)
        {
            return await _dataBaseLayer.SoftDeleteAssignedFaculty(facultyAssignId);
        }
    }


}
