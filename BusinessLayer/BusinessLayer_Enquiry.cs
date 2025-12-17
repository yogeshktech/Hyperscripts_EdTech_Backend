using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Enquiry
    {
        Task<IActionResult> ContactEnquiry(IFormCollection form);
        Task<IActionResult> GetAllEnquiries();
        Task<IActionResult> GetEnquiryById(int id);
        Task<IActionResult> DeleteEnquiry(int id);

        //Carrer job 
        Task<IActionResult> Careerjob(IFormCollection form);
        Task<IActionResult> GetAllResume();
        Task<IActionResult> GetCareerById(int id);
        Task<IActionResult> DeleteCareer(int id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Enquiry { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> ContactEnquiry(IFormCollection form)
        {
            return await _dataBaseLayer.ContactEnquiry(form);
        }

        public async Task<IActionResult> GetAllEnquiries()
        {
            return await _dataBaseLayer.GetAllEnquiries();
        }

        public async Task<IActionResult> GetEnquiryById(int id)
        {
            return await _dataBaseLayer.GetEnquiryById(id);
        }

        public async Task<IActionResult> DeleteEnquiry(int id)
        {
            return await _dataBaseLayer.DeleteEnquiry(id);
        }

        //Carrer Job 

        public async Task<IActionResult> Careerjob(IFormCollection form)
        {
            return await _dataBaseLayer.Careerjob(form);
        }

       

        public async Task<IActionResult> GetAllResume()
        {
            return await _dataBaseLayer.GetAllResume();
        }

        public async Task<IActionResult> GetCareerById(int id)
        {
            return await _dataBaseLayer.GetResumeById(id);
        }

        public async Task<IActionResult> DeleteCareer(int id)
        {
            return await _dataBaseLayer.DeleteCareer(id);
        }
    }
}
