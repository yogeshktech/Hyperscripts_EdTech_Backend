using CareerCracker.Areas.Identity.Data;

namespace CareerCracker.BusinessLayer
{
    public interface IBusinessLayer_Admin
    {
        Task<object> AddUser(IFormCollection form);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Admin
    {

    }

    public partial class BusinessLayer
    {
        public async Task<object> AddUser(IFormCollection form)
        {
            bool success = false;
            string message = string.Empty;

            if (_roleManager == null)
                return new { success = false, message = "_roleManager is null – check dependency injection" };

            if (_userManager == null)
                return new { success = false, message = "_userManager is null – check dependency injection" };

            try
            {
                // 🟢 Get form fields
                string email = form["email"];
                string password = form["password"]; // ⬅️ use form password directly
                string firstname = form["firstname"];
                string lastname = form["lastname"];
                string phone = form["phone"];
                var roles = form["role"].ToList();

                // 🟢 Basic validation
                if (string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(firstname) ||
                    string.IsNullOrEmpty(lastname) ||
                    string.IsNullOrEmpty(phone) ||
                    !roles.Any())
                {
                    return new { success = false, message = "One or more required fields are missing." };
                }

                // 🟢 Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return new { success = false, message = "Email address already exists." };
                }

                // 🟢 Create new user
                var user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FirstName = firstname,
                    LastName = lastname,
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreateDate = DateTime.UtcNow,
                    userType = 1,
                    sourcetype = "internal"
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    var availableRoles = _roleManager.Roles.Select(r => r.Name).ToList();

                    foreach (var role in roles)
                    {
                        if (!string.IsNullOrEmpty(role) &&
                            availableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                        {
                            await _userManager.AddToRoleAsync(user, role);
                        }
                    }

                    success = true;
                    message = "User created successfully.";
                }
                else
                {
                    message = string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Error: {ex.Message}";
            }

            return new { success, message };
        }
    }
}
