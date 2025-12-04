using CareerCracker.Areas.Identity.Data;
using CareerCracker.Other;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace CareerCracker.UserManagement
{
    public class ResetPasswordModel
    {
        [Microsoft.Build.Framework.Required] public string UserEmail { get; set; }
        [Microsoft.Build.Framework.Required] public string Token { get; set; }

        [Microsoft.Build.Framework.Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Microsoft.Build.Framework.Required, DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }

        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string ChangedBy { get; set; }
    }
    public partial interface IApplicationUserManagement
    {
        Task<bool> AddUser(ApplicationUser user, string password);
        Task MasterConfiguration();
        Task<Tuple<bool, string>> ResetPasswordAsync(ResetPasswordModel model);

    }

    public class ApplicationUserManagement : IApplicationUserManagement
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string DbCon;

        public ApplicationUserManagement(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, IConfiguration configuration,
        RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            //_configuration = configuration;
            _roleManager = roleManager;
            this.DbCon = this._configuration.GetConnectionString("AppDbContextConnection");
        }

        public async Task<bool> AddUser(ApplicationUser user, string password)
        {
            if ((_userManager.FindByNameAsync(user.UserName.Trim()).Result == null))
            {
                DateTime dateTime = DateTime.Now;
                DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime);
                long unixTimestamp = dateTimeOffset.ToUnixTimeSeconds();


                user.UserName = user.UserName.Trim().ToLower();
                user.Email = user.Email.Trim().ToLower();
                user.EmailConfirmed = true;
                user.PasswordChangeTime = unixTimestamp;
                IdentityResult result = _userManager.CreateAsync(user, password).Result;
                if (result.Succeeded)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public async Task MasterConfiguration()
        {
            try
            {
                IdentityResult roleResult;
                var roleCheck = await _roleManager.RoleExistsAsync("ADMIN");
                if (!roleCheck)
                {
                    roleResult = await _roleManager.CreateAsync(new IdentityRole() { Name = "ADMIN" });
                }

                roleCheck = await _roleManager.RoleExistsAsync("SUPERADMIN");
                if (!roleCheck)
                {
                    roleResult = await _roleManager.CreateAsync(new IdentityRole() { Name = "SUPERADMIN" });
                }

                roleCheck = await _roleManager.RoleExistsAsync("USER");
                if (!roleCheck)
                {
                    roleResult = await _roleManager.CreateAsync(new IdentityRole() { Name = "USER" });
                }


                ApplicationUser appUser = await _userManager.FindByNameAsync("praveenbabu7300@gmail.com");
                if (appUser == null)
                {
                    ApplicationUser applicationUser = new Areas.Identity.Data.ApplicationUser()
                    {
                        OrgId = "AA01",
                        UserName = "praveenbabu7300@gmail.com",
                        Email = "praveenbabu7300@gmail.com",
                        PhoneNumber = "0000000000",
                        IsActive = true,
                        FirstName = "PRAVEEN",
                        LastName = "BABU",
                        AccessKey = SecretHasher.Hash("July@1234"),
                        CreateDate = DateTime.UtcNow,
                        userType = 0,
                        signupsource = "TEST",
                        sourcetype = "TEST",
                        address = "noida"
                    };

                    bool check = await AddUser(applicationUser, "July@1234");
                    if (check == true)
                    {
                        List<string> Mrole = new List<string>() { "SUPERADMIN", "ADMIN", "USER" };
                        foreach (var role in Mrole)
                        {
                            await _userManager.AddToRoleAsync(applicationUser, role);
                        }

                        //await _databaseLayer.SetFreeCredits(applicationUser.Email, applicationUser.OrgId, 9000);
                    }
                }
                else
                {
                    List<string> Mrole = new List<string>() { "SUPERADMIN", "ADMIN" };
                    foreach (var role in Mrole)
                    {
                        await _userManager.AddToRoleAsync(appUser, role);
                    }
                    // await _databaseLayer.SetFreeCredits(appUser.Email, appUser.OrgId, 9000);
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }
        }
        public async Task<Tuple<bool, string>> ResetPasswordAsync(ResetPasswordModel model)
        {
            bool success = true;
            string errormessage = string.Empty;
            string dtoken = HttpUtility.UrlDecode(model.Token);
            ApplicationUser user = await _userManager.FindByNameAsync(model.UserEmail);
            if (user != null)
            {
                IdentityResult result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (result.Succeeded)
                {
                    DateTime dateTime = DateTime.Now;
                    DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime);
                    long unixTimestamp = dateTimeOffset.ToUnixTimeSeconds();

                    user.PasswordChangeTime = unixTimestamp;
                    user.PasswordChangeBy = model.ChangedBy;
                    int reset = await UpdateUser(user);
                }
                else
                {
                    var errlist = (from s in result.Errors
                                   select s.Description).ToList();


                    success = false;
                    errormessage = string.Join(Environment.NewLine, errlist);
                }
            }
            else
            {
                success = false;
                errormessage = "User not found.";
            }

            Tuple<bool, string> output = new Tuple<bool, string>(success, errormessage);
            return output;
        }
        public async Task<int> UpdateUser(ApplicationUser user)
        {
            DateTime dateTime = DateTime.Now;
            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime);
            long unixTimestamp = dateTimeOffset.ToUnixTimeSeconds();
            user.PasswordChangeTime = unixTimestamp;
            IdentityResult result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
