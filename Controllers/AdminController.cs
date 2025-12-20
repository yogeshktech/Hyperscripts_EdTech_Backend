using CareerCracker.Areas.Identity.Data;
using CareerCracker.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CareerCracker.Controllers
{
    public class AdminController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IBusinessLayer businessLayer, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<AdminController> logger, IConfiguration configuration)
        {
            this._businessLayer = businessLayer;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._roleManager = roleManager;
            _logger = logger;
        }

        [Route("add-user")]
        [HttpPost]
        public async Task<IActionResult> AddUser(IFormCollection form)
        {
            var result = await _businessLayer.AddUser(form);
            return Ok(result);
        }


        [Route("login")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login()
        {
            try
            {
                _logger.LogInformation("Login endpoint called");
                var form = await Request.ReadFormAsync();
                if (form == null)
                {
                    _logger.LogWarning("Form data is null");
                    return Unauthorized(new { success = false, message = "Form data is required" });
                }
                string username = form["email"];
                string password = form["password"];
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Email or password missing");
                    return Unauthorized(new { success = false, message = "Email and password are required" });
                }
                var user = await AuthenticateUser(username, password, "user");
                if (user == null)
                {
                    _logger.LogWarning("Invalid credentials for email: {Email}", username);
                    return Unauthorized(new { success = false, message = "Invalid credentials" });
                }
                var tokenString = await GenerateJSONWebToken(user);
                return Ok(new { success = true, token = tokenString.Item1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { success = false, message = $"Error during login: {ex.Message}" });
            }
        }

        private async Task<ApplicationUser> AuthenticateUser(string email, string password, string app)
        {
            _logger.LogInformation("Authenticating user with email: {Email}", email);
            ApplicationUser appUser = await _userManager.FindByNameAsync(email);
            if (appUser != null && appUser.IsActive)
            {
                var result = await _signInManager.PasswordSignInAsync(appUser, password, false, false);
                return result.Succeeded ? appUser : null;
            }
            _logger.LogWarning("User not found or inactive for email: {Email}", email);
            return null;
        }
        private async Task<Tuple<string, string>> GenerateJSONWebToken(ApplicationUser userInfo)
        {
            _logger.LogInformation("Generating JWT for user: {Email}", userInfo.Email);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Name, userInfo.FirstName),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                new Claim("UserName", userInfo.UserName),
                new Claim("UserEmail", userInfo.Email),
                new Claim("FirstName", userInfo.FirstName),
                new Claim("LastName", userInfo.LastName),
                new Claim("UserOrg", userInfo.OrgId),
                new Claim("AccessKey", userInfo.AccessKey),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };
            var userRoles = await _userManager.GetRolesAsync(userInfo);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddYears(1),
                signingCredentials: credentials);
            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);
            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);
            _logger.LogInformation("JWT generated successfully for user: {Email}", userInfo.Email);
            return new Tuple<string, string>(accessTokenString, refreshTokenString);
        }

        [Route("user-delete")]
        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        //public async Task<IActionResult> DeleteUser(IFormCollection form)
        //{
        //    try
        //    {
        //        var email = form["email"].ToString().Trim();

        //        if (string.IsNullOrWhiteSpace(email))
        //            return BadRequest(new { success = false, message = "Email is required" });

        //        var normalizedEmail = _userManager.NormalizeEmail(email);

        //        var user = await _userManager.Users
        //            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        //        if (user == null)
        //            return NotFound(new { success = false, message = "User not found" });

        //        var result = await _userManager.DeleteAsync(user);

        //        if (result.Succeeded)
        //            return Ok(new { success = true, message = "User deleted successfully" });

        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = "Error deleting user",
        //            errors = result.Errors
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Delete user error");
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        public async Task<IActionResult> DeleteUser(IFormCollection form)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(form["email"]))
                    return BadRequest(new { success = false, message = "email is required" });

                // Optional: Last name validation
                // if (string.IsNullOrWhiteSpace(form["LastName"]))
                //     return BadRequest(new { success = false, message = "Last name is required" });

                return await _businessLayer.DeleteUser(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [Route("user-update/{userId}")]
        [HttpPost]
        [Authorize(Roles = "SUPERADMIN,USER")]
        public async Task<IActionResult> UpdateUser(Guid userId, IFormCollection form)
        {
            try
            {
                string firstName = form["firstname"].ToString().Trim();
                string lastName = form["lastname"].ToString().Trim();
                string phone = form["phone"].ToString().Trim();
                var roles = form["role"].ToList(); // multiple roles

                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                // 1️⃣ Update basic fields
                user.FirstName = firstName;
                user.LastName = lastName;
                user.PhoneNumber = phone;

                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "User update failed",
                        errors = updateResult.Errors
                    });

                // 2️⃣ Update Roles (SAFE way)
                var existingRoles = await _userManager.GetRolesAsync(user);

                // Remove old roles
                var rolesToRemove = existingRoles.Except(roles).ToList();
                if (rolesToRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                // Add new roles
                var rolesToAdd = roles.Except(existingRoles).ToList();
                foreach (var role in rolesToAdd)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                        await _roleManager.CreateAsync(new IdentityRole(role));

                    await _userManager.AddToRoleAsync(user, role);
                }

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [Route("user-status/{userId}")]
        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> ToggleUserStatus(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return NotFound(new { success = false, message = "User not found" });
                }

                // 🔁 TOGGLE STATUS
                user.IsActive = !user.IsActive;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogError("Error toggling status for userId: {UserId}", userId);
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Failed to update user status",
                        errors = result.Errors
                    });
                }

                string action = user.IsActive ? "activated" : "deactivated";

                _logger.LogInformation(
                    "User {Action} successfully. UserId: {UserId}",
                    action, userId
                );

                return Ok(new
                {
                    success = true,
                    message = $"User {action} successfully",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while toggling user status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error while toggling user status"
                });
            }
        }



        [Route("user-get")]
        [HttpGet]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> GetUser([FromQuery] string? email, [FromQuery] string? id)
        {
            try
            {
                // ================= GET BY EMAIL =================
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                        return NotFound(new { success = false, message = "User not found" });

                    var roles = await _userManager.GetRolesAsync(user);

                    if (!roles.Contains("USER"))
                        return NotFound(new { success = false, message = "User is not USER role" });

                    return Ok(new
                    {
                        success = true,
                        user = MapUser(user, roles)
                    });
                }

                // ================= GET BY ID =================
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user == null)
                        return NotFound(new { success = false, message = "User not found" });

                    var roles = await _userManager.GetRolesAsync(user);

                    if (!roles.Contains("USER"))
                        return NotFound(new { success = false, message = "User is not USER role" });

                    return Ok(new
                    {
                        success = true,
                        user = MapUser(user, roles)
                    });
                }

                // ================= GET ALL USERS WITH ROLE = USER =================
                var usersInRole = await _userManager.GetUsersInRoleAsync("USER");

                var userList = usersInRole.Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.UserName,
                    u.PhoneNumber,
                    u.OrgId,
                    u.AccessKey,
                    u.IsActive,
                    Roles = new[] { "USER" }
                }).ToList();

                return Ok(new
                {
                    success = true,
                    users = userList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching users");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ================= HELPER =================
        private object MapUser(ApplicationUser user, IList<string> roles)
        {
            return new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.UserName,
                user.PhoneNumber,
                user.OrgId,
                user.AccessKey,
                user.IsActive,
                Roles = roles
            };
        }




        [Route("user-get/{id}")]
        [HttpGet]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { success = false, message = "User ID is required" });
                }

                var user = await _userManager.Users
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        // Identity fields
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.EmailConfirmed,
                        u.PhoneNumber,
                        u.PhoneNumberConfirmed,
                        u.TwoFactorEnabled,
                        u.LockoutEnabled,
                        u.LockoutEnd,
                        u.AccessFailedCount,

                        // Custom fields (your added columns)
                        u.FirstName,
                        u.LastName,
                        u.OrgId,
                        u.AccessKey,
                        u.IsActive,

                        // Audit fields (if added)
                        u.created_at,
                        u.updated_at
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(
                    await _userManager.FindByIdAsync(id)
                );

                return Ok(new
                {
                    success = true,
                    user,
                    roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by ID");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error fetching user: {ex.Message}"
                });
            }
        }

    }
}
