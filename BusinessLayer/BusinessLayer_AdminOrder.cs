using Microsoft.AspNetCore.Mvc;
using CareerCracker.Models;
using CareerCracker.DataBaseLayer;
namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_AdminOrder
    {
        Task<List<AdminOrderModel>> GetAllAdminOrders();
    }

    public partial interface IBusinessLayer : IBusinessLayer_AdminOrder { }

    public partial class BusinessLayer
    {

        public async Task<List<AdminOrderModel>> GetAllAdminOrders()
        {
            return await _dataBaseLayer.GetAllAdminOrders();
        }

    }

}