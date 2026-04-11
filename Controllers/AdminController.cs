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
using Microsoft.AspNetCore.Hosting;


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
        private readonly IWebHostEnvironment _env;

        public AdminController(IBusinessLayer businessLayer, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<AdminController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            this._businessLayer = businessLayer;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._roleManager = roleManager;
            _logger = logger;
            _env = env;

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
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Id),
                new Claim(ClaimTypes.NameIdentifier, userInfo.Id),
                new Claim(JwtRegisteredClaimNames.Name, userInfo.FirstName ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email ?? ""),
                new Claim("UserName", userInfo.UserName ?? ""),
                new Claim("UserEmail", userInfo.Email ?? ""),
                new Claim("FirstName", userInfo.FirstName ?? ""),
                new Claim("LastName", userInfo.LastName ?? ""),
                new Claim("UserOrg", userInfo.OrgId ?? ""),
                new Claim("AccessKey", userInfo.AccessKey ?? ""),
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
        [Authorize(Roles = "SUPERADMIN,USER,ADMIN")]
        public async Task<IActionResult> UpdateUser(Guid userId, IFormCollection form)
        {
            try
            {
                // ===============================
                // 1️⃣ Read Inputs
                // ===============================
                string firstName = form["firstname"];
                string lastName = form["lastname"];
                string position = form["position"];
                string specialization = form["specialization"];
                string gender = form["gender"];
                string address = form["address"];
                string subject = form["subject"];
                string phone = form["phone"];
                string experienceInput = form["experience"];
                string salaryInput = form["salary"];
                string dobInput = form["dob"];

                var roles = form["role"].ToList();
                IFormFile image = form.Files.FirstOrDefault();

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                // ===============================
                // 2️⃣ Experience Logic (TEXT)
                // ===============================
                //if (!string.IsNullOrWhiteSpace(experienceInput) &&
                //    decimal.TryParse(experienceInput, out decimal exp))
                //{
                //    if (exp < 1)
                //        user.experience = $"{exp * 12} Months";
                //    else if (exp == 1)
                //        user.experience = "1 Year";
                //    else
                //        user.experience = $"{exp} Years";
                //}
                //else
                //{
                //    user.experience = experienceInput?.Trim();
                //}

                // ===============================
                // 3️⃣ Salary (numeric)
                // ===============================
                user.Salary = decimal.TryParse(salaryInput, out var sal) ? sal : null;

                // ===============================
                // 4️⃣ DateOfBirth (UTC FIX)
                // ===============================
                if (DateTime.TryParse(dobInput, out DateTime dob))
                {
                    user.DateOfBirth = DateTime.SpecifyKind(dob, DateTimeKind.Utc);
                }
                else
                {
                    user.DateOfBirth = null;
                }

                // ===============================
                // 5️⃣ Other Fields
                // ===============================
                user.FirstName = firstName?.Trim();
                user.LastName = lastName?.Trim();
                //user.position = position?.Trim();
                user.specialization = specialization?.Trim();
                user.Gender = gender?.Trim();
                user.Address = address?.Trim();
                user.Subject = subject?.Trim();
                user.PhoneNumber = phone?.Trim();

                // ===============================
                // 6️⃣ Image Upload
                // ===============================
                if (image != null && image.Length > 0)
                {
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png",".svg",".webp" };
                    var ext = Path.GetExtension(image.FileName).ToLower();

                    if (!allowedExt.Contains(ext))
                        return BadRequest(new { success = false, message = "Invalid image type" });

                    string uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "users");
                    Directory.CreateDirectory(uploadRoot);

                    if (!string.IsNullOrEmpty(user.profile_image))
                    {
                        string oldPath = Path.Combine(_env.WebRootPath, user.profile_image.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    string fileName = $"{Guid.NewGuid()}{ext}";
                    string filePath = Path.Combine(uploadRoot, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await image.CopyToAsync(stream);

                    user.profile_image = $"/uploads/users/{fileName}";
                }

                // ===============================
                // 7️⃣ Update User
                // ===============================
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "User update failed",
                        errors = updateResult.Errors
                    });
                }

                // ===============================
                // 8️⃣ Update Roles
                // ===============================
                var existingRoles = await _userManager.GetRolesAsync(user);

                var rolesToRemove = existingRoles.Except(roles).ToList();
                if (rolesToRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

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
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                });
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
                // GET BY EMAIL
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                        return NotFound(new { success = false, message = "User not found" });

                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Contains("USER"))
                        return NotFound(new { success = false, message = "User is not USER role" });

                    return Ok(new { success = true, user = MapUser(user, roles) });
                }

                // GET BY ID
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user == null)
                        return NotFound(new { success = false, message = "User not found" });

                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Contains("USER"))
                        return NotFound(new { success = false, message = "User is not USER role" });

                    return Ok(new { success = true, user = MapUser(user, roles) });
                }

                // GET ALL USERS
                var usersInRole = await _userManager.GetUsersInRoleAsync("USER");

                var userList = usersInRole.Select(u => new
                {
                    id = u.Id,
                    firstName = u.FirstName ?? "",
                    lastName = u.LastName ?? "",
                    fullName = string.IsNullOrWhiteSpace(u.FirstName) && string.IsNullOrWhiteSpace(u.LastName)
                                ? (u.UserName ?? u.Email ?? "N/A")
                                : $"{u.FirstName} {u.LastName}".Trim(),
                    email = u.Email,
                    userName = u.UserName,
                    phoneNumber = u.PhoneNumber,
                    orgId = u.OrgId,
                    accessKey = u.AccessKey,
                    isActive = u.IsActive,
                    profileImage = u.profile_image,
                    profileImageUrl = ResolveProfileImageUrl(u.profile_image),
                    roles = new[] { "USER" }
                }).ToList();

                return Ok(new { success = true, users = userList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching users");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                    // You can temporarily add: , detail = ex.InnerException?.Message 
                });
            }
        }

        // ================= HELPER =================
        private string? ResolveProfileImageUrl(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;
            var s = stored.Trim();
            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return s;
            if (s.StartsWith('/'))
                return $"{Request.Scheme}://{Request.Host.Value}{s}";
            return $"{Request.Scheme}://{Request.Host.Value}/{s.TrimStart('/')}";
        }

        private object MapUser(ApplicationUser user, IList<string> roles)
        {
            return new
            {
                id = user.Id,
                firstName = user.FirstName ?? "",
                lastName = user.LastName ?? "",
                fullName = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
                            ? (user.UserName ?? user.Email ?? "N/A")
                            : $"{user.FirstName} {user.LastName}".Trim(),
                email = user.Email,
                userName = user.UserName,
                phoneNumber = user.PhoneNumber,
                orgId = user.OrgId,
                accessKey = user.AccessKey,
                isActive = user.IsActive,
                profileImage = user.profile_image,
                profileImageUrl = ResolveProfileImageUrl(user.profile_image),
                roles = roles
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
                        u.updated_at,

                        profileImage = u.profile_image
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(
                    await _userManager.FindByIdAsync(id)
                );

                var profileImageUrl = ResolveProfileImageUrl(user.profileImage);

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.EmailConfirmed,
                        user.PhoneNumber,
                        user.PhoneNumberConfirmed,
                        user.TwoFactorEnabled,
                        user.LockoutEnabled,
                        user.LockoutEnd,
                        user.AccessFailedCount,
                        user.FirstName,
                        user.LastName,
                        user.OrgId,
                        user.AccessKey,
                        user.IsActive,
                        user.created_at,
                        user.updated_at,
                        user.profileImage,
                        profileImageUrl
                    },
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
