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
        [Authorize(Roles = "ADMIN,SUPERADMIN,USER")] // Only admins can delete users
        public async Task<IActionResult> DeleteUser(IFormCollection form)
        {
            try
            {
                var email = form["email"];
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email is required to delete user");
                    return BadRequest(new { success = false, message = "Email is required" });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return NotFound(new { success = false, message = "User not found" });
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User deleted successfully: {Email}", email);
                    return Ok(new { success = true, message = "User deleted successfully" });
                }
                else
                {
                    _logger.LogError("Error deleting user: {Email}", email);
                    return StatusCode(500, new { success = false, message = "Error deleting user", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while deleting user");
                return StatusCode(500, new { success = false, message = $"Error during deletion: {ex.Message}" });
            }
        }

        [Route("user-update")]
        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN,USER")]
        public async Task<IActionResult> UpdateUser(IFormCollection form)
        {
            try
            {
                // Extract fields from form
                string email = form["email"];
                string firstname = form["firstname"];
                string lastname = form["lastname"];
                string phone = form["phone"];
                var roles = form["role"].ToList(); // List of roles

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email is required to update user");
                    return BadRequest(new { success = false, message = "Email is required" });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Update basic fields
                if (!string.IsNullOrEmpty(firstname)) user.FirstName = firstname;
                if (!string.IsNullOrEmpty(lastname)) user.LastName = lastname;
                if (!string.IsNullOrEmpty(phone)) user.PhoneNumber = phone;

                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToAdd = roles.Except(currentRoles).ToList();
                var rolesToRemove = currentRoles.Except(roles).ToList();

                if (rolesToAdd.Any())
                {
                    var addResult = await _roleManager.Roles.AnyAsync(r => rolesToAdd.Contains(r.Name))
                        ? await _userManager.AddToRolesAsync(user, rolesToAdd)
                        : null;
                    if (addResult != null && !addResult.Succeeded)
                        return StatusCode(500, new { success = false, message = "Error adding roles", errors = addResult.Errors });
                }

                if (rolesToRemove.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                        return StatusCode(500, new { success = false, message = "Error removing roles", errors = removeResult.Errors });
                }

                // Save updates
                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    _logger.LogInformation("User updated successfully: {Email}", email);
                    return Ok(new { success = true, message = "User updated successfully" });
                }
                else
                {
                    _logger.LogError("Error updating user: {Email}", email);
                    return StatusCode(500, new { success = false, message = "Error updating user", errors = updateResult.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating user");
                return StatusCode(500, new { success = false, message = $"Error during update: {ex.Message}" });
            }
        }

        [Route("user-status")]
        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> SetUserStatus(IFormCollection form)
        {
            try
            {
                string email = form["email"];
                string status = form["isActive"]; // "true" or "false"

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(status))
                {
                    _logger.LogWarning("Email and isActive status are required");
                    return BadRequest(new { success = false, message = "Email and isActive are required" });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Update IsActive
                bool isActive = status.ToLower() == "true";
                user.IsActive = isActive;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    string action = isActive ? "activated" : "deactivated";
                    _logger.LogInformation("User {Action} successfully: {Email}", action, email);
                    return Ok(new { success = true, message = $"User {action} successfully" });
                }
                else
                {
                    _logger.LogError("Error updating user status: {Email}", email);
                    return StatusCode(500, new { success = false, message = "Error updating user status", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating user status");
                return StatusCode(500, new { success = false, message = $"Error during status update: {ex.Message}" });
            }
        }


        [Route("user-get")]
        [HttpGet]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> GetUser([FromQuery] string? email, [FromQuery] string? id)
        {
            try
            {
                if (!string.IsNullOrEmpty(email))
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                        return NotFound(new { success = false, message = "User not found" });

                    var roles = await _userManager.GetRolesAsync(user);
                    return Ok(new
                    {
                        success = true,
                        user = new
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
                        }
                    });
                }
                else if (!string.IsNullOrEmpty(id))
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user == null)
                        return NotFound(new { success = false, message = "User not found" });

                    var roles = await _userManager.GetRolesAsync(user);
                    return Ok(new
                    {
                        success = true,
                        user = new
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
                        }
                    });
                }
                else
                {
                    // Get all users
                    var users = _userManager.Users.ToList();
                    var userList = new List<object>();
                    foreach (var u in users)
                    {
                        var userRoles = await _userManager.GetRolesAsync(u);
                        userList.Add(new
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
                            Roles = userRoles
                        });
                    }

                    return Ok(new { success = true, users = userList });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching user(s)");
                return StatusCode(500, new { success = false, message = $"Error fetching user(s): {ex.Message}" });
            }
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
