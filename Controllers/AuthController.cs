using CareerCracker.Areas.Identity.Data;
using CareerCracker.BusinessLayer;
using CareerCracker.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly IApplicationUserManagement _applicationUserManagement;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly IBusinessLayer _businessLayer;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IApplicationUserManagement applicationUserManagement,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IBusinessLayer businessLayer,
            ILogger<AuthController> logger)
        {
            _applicationUserManagement = applicationUserManagement;
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _businessLayer = businessLayer;
            _logger = logger;
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
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
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
            new Claim(
                JwtRegisteredClaimNames.Iat,
                ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            )
        };

            var userRoles = await _userManager.GetRolesAsync(userInfo);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddYears(1),
                signingCredentials: credentials);
            var refreshToken = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);
            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);
            _logger.LogInformation("JWT generated successfully for user: {Email}", userInfo.Email);
            return new Tuple<string, string>(accessTokenString, refreshTokenString);
        }

        [Route("forgotpassword")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword()
        {
            try
            {
                _logger.LogInformation("ForgotPassword endpoint called");
                var form = await Request.ReadFormAsync();
                if (form == null || !form.ContainsKey("email"))
                {
                    _logger.LogWarning("Form data or email missing");
                    return BadRequest(new { success = false, message = "Email is required" });
                }
                var result = await _businessLayer.ResetPassword(form);
                return Ok(new { success = result, message = result ? "Password reset email sent" : "Failed to initiate password reset" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating password reset");
                return StatusCode(500, new { success = false, message = $"Error initiating password reset: {ex.Message}" });
            }
        }

        [Route("resetpassword")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ConfirmResetPassword()
        {
            try
            {
                _logger.LogInformation("ConfirmResetPassword endpoint called");
                var form = await Request.ReadFormAsync();
                if (form == null || !form.ContainsKey("email") || !form.ContainsKey("token") || !form.ContainsKey("newPassword"))
                {
                    _logger.LogWarning("Form data, email, token, or newPassword missing");
                    return BadRequest(new { success = false, message = "Email, token, and new password are required" });
                }
                var result = await _businessLayer.ConfirmResetPassword(form);
                return Ok(new { success = result, message = result ? "Password reset successful" : "Failed to reset password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { success = false, message = $"Error resetting password: {ex.Message}" });
            }
        }

        [HttpPost("resetpasswordadmin")]
        [Authorize]   // REQUIRED
        public async Task<IActionResult> ResetPasswordAdmin()
        {
            // ---------------------------
            // 1️⃣ Extract username/email from JWT
            // ---------------------------
            var usernameClaim =
                User.FindFirst(ClaimTypes.Email) ??
                User.FindFirst("email") ??
                User.FindFirst(ClaimTypes.Name) ??
                User.FindFirst("name") ??
                User.FindFirst("unique_name");

            if (usernameClaim == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "No valid user identification found in JWT token."
                });
            }

            string loggedInUser = usernameClaim.Value;

            // ---------------------------
            // 2️⃣ Read form data safely
            // ---------------------------
            var form = await Request.ReadFormAsync();
            if (form == null || form.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Form data is required."
                });
            }

            // ---------------------------
            // 3️⃣ Call business layer
            // ---------------------------
            var result = await _businessLayer.ResetPasswordByAdmin(form, loggedInUser);
            return Ok(result);
        }



    }
}
