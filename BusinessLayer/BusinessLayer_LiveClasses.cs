using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_LiveClasses
    {
        Task<IActionResult> CreateLiveClass(int batchId, IFormCollection form);
        Task<IActionResult> UpdateLiveClass(int liveClassId, IFormCollection form);
        Task<IActionResult> GetAllLiveClasses();
        Task<IActionResult> GetLiveClassesByBatch(int batchId);
        Task<IActionResult> HardDeleteLiveClass(int liveClassId);
        Task<IActionResult> CreateLiveClassAttendance(int liveClassId, string userEmail);
        Task<IActionResult> UpdateLiveClassAttendance(int attendanceId);
        Task<IActionResult> GetAttendanceByLiveClass(int liveClassId);
        Task<IActionResult> GetAttendanceByUser(Guid userId);
        Task<IActionResult> DeleteLiveClassAttendance(int attendanceId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_LiveClasses { }

    public partial class BusinessLayer
    {
        public async Task<IActionResult> CreateLiveClass(int batchId, IFormCollection form)
        {
            return await _dataBaseLayer.CreateLiveClass(batchId, form);
        }

        public async Task<IActionResult> UpdateLiveClass(int liveClassId, IFormCollection form)
        {
            return await _dataBaseLayer.UpdateLiveClass(liveClassId,form);
        }
        public async Task<IActionResult> GetAllLiveClasses()
        {
            return await _dataBaseLayer.GetAllLiveClasses();
        }
        public async Task<IActionResult> GetLiveClassesByBatch(int batchId)
        {
            return await _dataBaseLayer.GetLiveClassesByBatch(batchId);
        }
        public async Task<IActionResult> HardDeleteLiveClass(int liveClassId)
        {
            return await _dataBaseLayer.HardDeleteLiveClass(liveClassId);
        }

        public async Task<IActionResult> CreateLiveClassAttendance(int liveClassId,string userEmail)
        {
            return await _dataBaseLayer.CreateLiveClassAttendance(liveClassId, userEmail);
        }

        public async Task<IActionResult> UpdateLiveClassAttendance(int attendanceId)
        {
            return await _dataBaseLayer.UpdateLiveClassAttendance(attendanceId);
        }

        public async Task<IActionResult> GetAttendanceByLiveClass(int liveClassId)
        {
            return await _dataBaseLayer.GetAttendanceByLiveClass(liveClassId);
        }

        public async Task<IActionResult> GetAttendanceByUser(Guid userId)
        {
            return await _dataBaseLayer.GetAttendanceByUser(userId);
        }

        public async Task<IActionResult> DeleteLiveClassAttendance(int attendanceId)
        {
            return await _dataBaseLayer.DeleteLiveClassAttendance(attendanceId);
        }
    }
}
