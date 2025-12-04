using CareerCracker.Areas.Identity.Data;
using UM = CareerCracker.UserManagement;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Auth
    {
        Task<bool> ResetPassword(IFormCollection form);
        Task<bool> ConfirmResetPassword(IFormCollection form);
        Task<object> ResetPasswordByAdmin(IFormCollection form, string userId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Auth
    {
    }

    public partial class BusinessLayer
    {
        public async Task<bool> ResetPassword(ApplicationUser user)
        {
            return true;
        }

        public async Task<bool> ResetPassword(IFormCollection form)
        {
            if (form == null || string.IsNullOrEmpty(form["email"]))
                return false;

            string email = form["email"];
            var user = await _userManager.FindByEmailAsync(email);
            return await ResetPassword(user);
        }

        public async Task<bool> ConfirmResetPassword(IFormCollection form)
        {
            var resetEmail = form["uid"];
            var token = form["token"];
            var pass = form["pass"];

            if (string.IsNullOrEmpty(resetEmail) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(pass))
                return false;

            ApplicationUser applicationUser = await _userManager.FindByEmailAsync(resetEmail);

            if (applicationUser == null)
                return false;

            // IMPORTANT FIX → Use the correct ResetPasswordModel class
            UM.ResetPasswordModel resetPasswordModel = new UM.ResetPasswordModel
            {
                UserEmail = applicationUser.UserName,
                NewPassword = pass,
                Token = token,
                ChangedBy = "User"
            };

            // Call the user manager reset password function
            var result = await _applicationUserManagement.ResetPasswordAsync(resetPasswordModel);

            return result.Item1;
        }

        public async Task<object> ResetPasswordByAdmin(IFormCollection form, string userIdentifier)
        {
            bool success = false;
            string message = string.Empty;

            // -----------------------------------------
            // 1️⃣ Determine if identifier is email or username
            // -----------------------------------------
            ApplicationUser creator =
                await _userManager.FindByEmailAsync(userIdentifier)
                ?? await _userManager.FindByNameAsync(userIdentifier);

            if (creator == null)
            {
                return new
                {
                    success = false,
                    message = "Invalid admin identity. Cannot reset password."
                };
            }

            // -----------------------------------------
            // 2️⃣ Extract form fields
            // -----------------------------------------
            string email = form["email"];
            string password = form["password"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return new
                {
                    success = false,
                    message = "Email and password are required."
                };
            }

            // -----------------------------------------
            // 3️⃣ Find user whose password to reset
            // -----------------------------------------
            ApplicationUser user =
                await _userManager.FindByEmailAsync(email)
                ?? await _userManager.FindByNameAsync(email);

            if (user == null)
            {
                return new
                {
                    success = false,
                    message = "User not found."
                };
            }

            // -----------------------------------------
            // 4️⃣ Generate reset token
            // -----------------------------------------
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // -----------------------------------------
            // 5️⃣ Create reset model
            // -----------------------------------------
            UM.ResetPasswordModel resetPasswordModel = new UM.ResetPasswordModel
            {
                UserEmail = user.UserName,
                NewPassword = password,
                Token = token,
                ChangedBy = creator.Email
            };

            // -----------------------------------------
            // 6️⃣ Execute reset
            // -----------------------------------------
            var result = await _applicationUserManagement.ResetPasswordAsync(resetPasswordModel);

            success = result.Item1;
            message = result.Item2;

            return new
            {
                success = success,
                message = message
            };
        }
    }
}
