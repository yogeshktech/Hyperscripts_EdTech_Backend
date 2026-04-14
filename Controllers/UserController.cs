using CareerCracker.Areas.Identity.Data;
using CareerCracker.BusinessLayer;
using CareerCracker.Models;
using CareerCracker.S3Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/user")]
    [Authorize(Roles = "USER,SUPERADMIN")]
    public class UserController : ControllerBase
    {
        private static readonly JsonSerializerOptions ProfileJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IBusinessLayer _businessLayer;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(
            IBusinessLayer businessLayer,
            UserManager<ApplicationUser> userManager)
        {
            _businessLayer = businessLayer;
            _userManager = userManager;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
                return user;

            var email =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("UserEmail")?.Value
                ?? User.FindFirst("email")?.Value;
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _userManager.FindByEmailAsync(email)
                   ?? await _userManager.FindByNameAsync(email);
        }

        private string? ResolveProfileImageUrl(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
                return null;
            var s = stored.Trim();
            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return s;
            if (s.StartsWith('/'))
                return $"{Request.Scheme}://{Request.Host.Value}{s}";
            return $"{Request.Scheme}://{Request.Host.Value}/{s.TrimStart('/')}";
        }

        private static string? BuildDisplayName(ApplicationUser user)
        {
            var n = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrEmpty(n) ? null : n;
        }

        private static object MapProfilePayload(ApplicationUser user, string? profileImageUrl, IEnumerable<string> roles)
        {
            return new
            {
                id = user.Id,
                email = user.Email,
                userName = user.UserName,
                displayName = BuildDisplayName(user),
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber,
                position = user.position,
                experience = user.experience,
                specialization = user.specialization,
                gender = user.Gender,
                address = user.Address,
                subject = user.Subject,
                salary = user.Salary,
                dateOfBirth = user.DateOfBirth,
                profileImage = user.profile_image,
                profileImageUrl,
                orgId = user.OrgId,
                isActive = user.IsActive,
                roles
            };
        }

        private void ApplyUpdateProfileRequest(ApplicationUser user, UpdateProfileRequest body)
        {
            if (body.FirstName != null)
                user.FirstName = string.IsNullOrWhiteSpace(body.FirstName) ? null : body.FirstName.Trim();
            if (body.LastName != null)
                user.LastName = string.IsNullOrWhiteSpace(body.LastName) ? null : body.LastName.Trim();
            if (body.PhoneNumber != null)
                user.PhoneNumber = string.IsNullOrWhiteSpace(body.PhoneNumber) ? null : body.PhoneNumber.Trim();
            if (body.Position != null)
                user.position = string.IsNullOrWhiteSpace(body.Position) ? null : body.Position.Trim();
            if (body.Experience != null)
                user.experience = string.IsNullOrWhiteSpace(body.Experience) ? null : body.Experience.Trim();
            if (body.Specialization != null)
                user.specialization = string.IsNullOrWhiteSpace(body.Specialization) ? null : body.Specialization.Trim();
            if (body.Gender != null)
                user.Gender = string.IsNullOrWhiteSpace(body.Gender) ? null : body.Gender.Trim();
            if (body.Address != null)
                user.Address = string.IsNullOrWhiteSpace(body.Address) ? null : body.Address.Trim();
            if (body.Subject != null)
                user.Subject = string.IsNullOrWhiteSpace(body.Subject) ? null : body.Subject.Trim();
            if (body.Salary.HasValue)
                user.Salary = body.Salary;
            if (body.ProfileImage != null || body.profile_image != null)
            {
                var img = body.ProfileImage ?? body.profile_image;
                user.profile_image = string.IsNullOrWhiteSpace(img) ? null : img.Trim();
            }

            if (body.DateOfBirth != null)
            {
                if (string.IsNullOrWhiteSpace(body.DateOfBirth))
                    user.DateOfBirth = null;
                else if (DateTime.TryParse(body.DateOfBirth, out var dob))
                    user.DateOfBirth = DateTime.SpecifyKind(dob, DateTimeKind.Utc);
            }

            user.updated_at = DateTime.UtcNow;
        }

        private async Task<IActionResult?> TrySaveProfileImageAsync(ApplicationUser user, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".svg", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
                return BadRequest(new { success = false, message = "Invalid image type" });

            await S3StorageHelper.DeleteStoredMediaAsync(user.profile_image);
            var uploadedUrl = await S3StorageHelper.UploadFileAsync(file, "users");
            if (string.IsNullOrWhiteSpace(uploadedUrl))
                return StatusCode(500, new { success = false, message = "Profile image upload failed." });

            user.profile_image = uploadedUrl;
            return null;
        }

        /// <summary>Current user profile (JWT required).</summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { success = false, message = "User not found in token" });

            var roles = await _userManager.GetRolesAsync(user);
            var url = ResolveProfileImageUrl(user.profile_image);
            return Ok(new { success = true, user = MapProfilePayload(user, url, roles) });
        }

        /// <summary>Update profile fields (JSON). Accepts either a flat object or the same shape as GET profile: <c>{ "user": { ... } }</c>.</summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfileForm([FromForm] IFormCollection form)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized(new { success = false, message = "User not found in token" });

                // ===============================
                // ✅ BASIC FIELDS
                // ===============================
                if (!string.IsNullOrWhiteSpace(form["FirstName"]))
                    user.FirstName = form["FirstName"];

                if (!string.IsNullOrWhiteSpace(form["LastName"]))
                    user.LastName = form["LastName"];

                if (!string.IsNullOrWhiteSpace(form["Email"]))
                {
                    var newEmail = form["Email"].ToString().Trim();
                    if (newEmail.Length > 256)
                    {
                        return BadRequest(new { success = false, message = "Email must be at most 256 characters." });
                    }

                    var existing = await _userManager.FindByEmailAsync(newEmail);
                    if (existing != null && !string.Equals(existing.Id, user.Id, StringComparison.Ordinal))
                    {
                        return BadRequest(new { success = false, message = "That email is already in use by another account." });
                    }

                    user.Email = newEmail;
                    user.UserName = newEmail; // keep username same as email
                }

                if (!string.IsNullOrWhiteSpace(form["PhoneNumber"]))
                    user.PhoneNumber = form["PhoneNumber"];

                // ===============================
                // ✅ PROFILE FIELDS
                // ===============================
                if (!string.IsNullOrWhiteSpace(form["position"]))
                    user.position = form["position"];

                if (!string.IsNullOrWhiteSpace(form["experience"]))
                    user.experience = form["experience"];

                if (!string.IsNullOrWhiteSpace(form["specialization"]))
                    user.specialization = form["specialization"];

                if (!string.IsNullOrWhiteSpace(form["Gender"]))
                    user.Gender = form["Gender"];

                if (!string.IsNullOrWhiteSpace(form["Address"]))
                    user.Address = form["Address"];

                if (!string.IsNullOrWhiteSpace(form["Subject"]))
                    user.Subject = form["Subject"];

                // ===============================
                // ✅ SALARY
                // ===============================
                if (decimal.TryParse(form["Salary"], out var salary))
                    user.Salary = salary;

                // ===============================
                // ✅ DATE OF BIRTH (UTC — required for PostgreSQL timestamptz / Npgsql)
                // ===============================
                if (form.ContainsKey("DateOfBirth"))
                {
                    var dobRaw = form["DateOfBirth"].ToString();
                    if (string.IsNullOrWhiteSpace(dobRaw))
                        user.DateOfBirth = null;
                    else if (DateTime.TryParse(dobRaw.Trim(), out var dob))
                        user.DateOfBirth = DateTime.SpecifyKind(dob, DateTimeKind.Utc);
                }

                // ===============================
                // ✅ IMAGE UPLOAD (S3)
                // ===============================
                var file = form.Files["profile_image"];

                if (file != null && file.Length > 0)
                {
                    var imagePath = await S3StorageHelper.UploadFileAsync(file, "users");

                    if (string.IsNullOrEmpty(imagePath))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Image upload failed"
                        });
                    }

                    // delete old image
                    if (!string.IsNullOrEmpty(user.profile_image))
                    {
                        await S3StorageHelper.DeleteFileAsync(user.profile_image);
                    }

                    user.profile_image = imagePath;
                }

                // ===============================
                // ✅ UPDATE TIMESTAMP
                // ===============================
                user.updated_at = DateTime.UtcNow;

                // ===============================
                // ✅ SAVE
                // ===============================
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Profile update failed",
                        errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                // ===============================
                // ✅ RESPONSE
                // ===============================
                var refreshed = await _userManager.FindByIdAsync(user.Id) ?? user;
                var roles = await _userManager.GetRolesAsync(refreshed);

                return Ok(new
                {
                    success = true,
                    message = "Profile updated",
                    user = MapProfilePayload(
                        refreshed,
                        ResolveProfileImageUrl(refreshed.profile_image),
                        roles
                    )
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message ?? ex.GetBaseException().Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>Change password while logged in: verifies current password, then sets the new one.</summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { success = false, message = "User not found in token" });

            if (!await _userManager.CheckPasswordAsync(user, model.OldPassword))
            {
                return BadRequest(new { success = false, message = "Current password is incorrect" });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password change failed",
                    errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            user.PasswordChangeTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            user.PasswordChangeBy = user.Email ?? user.UserName;
            user.updated_at = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(new { success = true, message = "Password changed successfully" });
        }

        /// <summary>Paid, confirmed orders for the logged-in user (purchase history).</summary>
        [HttpGet("purchase-history")]
        public async Task<IActionResult> PurchaseHistory()
        {
            var userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("UserEmail")?.Value
                ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized(new { success = false, message = "Email claim not found in token" });

            return await _businessLayer.GetPurchaseHistory(userEmail);
        }

        [Route("my-course")]
        [HttpGet]
        public async Task<IActionResult> MyCourses()
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                return await _businessLayer.MyCourses(userEmail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("my-batch/{courseId}")]
        [HttpGet]
        public async Task<IActionResult> MyBatch(int courseId)
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                return await _businessLayer.MyBatch(courseId, userEmail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("live-class-attendance/{liveClassId}")]
        [HttpPost]
        public async Task<IActionResult> CreateLiveClassAttendance(int liveClassId)
        {
            try
            {
                string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();
                return await _businessLayer.CreateLiveClassAttendance(liveClassId, userEmail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("update-class-attendance/{attendanceId}")]
        [HttpPut]
        public async Task<IActionResult> UpdateLiveClassAttendance(int attendanceId)
        {
            try
            {
                return await _businessLayer.UpdateLiveClassAttendance(attendanceId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("get-recording-class/{batchId}")]
        [HttpGet]
        public async Task<IActionResult> GetRecordingClass(int batchId)
        {
            try
            {
                return await _businessLayer.GetRecordingClass(batchId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("get-batch-by-userid")]
        [HttpDelete]
        public async Task<IActionResult> GetBatchByUserId()
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                return await _businessLayer.GetBatchByUserId(userEmail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Route("get-all-live-class/{batchId}")]
        [HttpGet]
        public async Task<IActionResult> GetLiveClassesByBatch(int batchId)
        {
            try
            {
                return await _businessLayer.GetLiveClassesByBatch(batchId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>Multipart/form profile update (legacy form field names).</summary>
        [Route("update-user-detail")]
        [HttpPost]
        public async Task<IActionResult> UpdateUserDetailByUserId(IFormCollection form)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized(new { success = false, message = "User not found in token" });

                var dto = new UpdateProfileRequest
                {
                    FirstName = form["firstName"].ToString(),
                    LastName = form["last_name"].ToString(),
                    PhoneNumber = form["phone_number"].ToString(),
                    Position = form["position"].ToString(),
                    Experience = form["experience"].ToString(),
                    Specialization = form["specialization"].ToString(),
                    Gender = form["gender"].ToString(),
                    Address = form["user_address"].ToString(),
                    Subject = form["subject"].ToString(),
                    DateOfBirth = form["dob"].ToString()
                };

                if (decimal.TryParse(form["salary"].ToString(), out var sal))
                    dto.Salary = sal;

                ApplyUpdateProfileRequest(user, dto);

                var imgErr = await TrySaveProfileImageAsync(user, form.Files["profile_image"] ?? form.Files.FirstOrDefault());
                if (imgErr != null)
                    return imgErr;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Profile update failed",
                        errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new
                {
                    success = true,
                    message = "Profile updated",
                    user = MapProfilePayload(user, ResolveProfileImageUrl(user.profile_image), roles)
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message ?? ex.GetBaseException().Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
