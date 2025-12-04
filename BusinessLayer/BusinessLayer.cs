using CareerCracker.Areas.Identity.Data;
using CareerCracker.DataBaseLayer;
using CareerCracker.UserManagement;
using Microsoft.AspNetCore.Identity;

namespace CareerCracker.BusinessLayer
{
    public partial interface IBusinessLayer
    {
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        private IWebHostEnvironment _env;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IApplicationUserManagement _applicationUserManagement;
        private readonly IDataBaseLayer _dataBaseLayer;
        public BusinessLayer(
            IWebHostEnvironment env, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration,
            IApplicationUserManagement applicationUserManagement, IDataBaseLayer dataBaseLayer,
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager
            )
        {
            this._env = env;
            this._scopeFactory = serviceScopeFactory;
            this._configuration = configuration;
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._applicationUserManagement = applicationUserManagement;
            this._dataBaseLayer = dataBaseLayer;

        }
    }
}
