using System.ComponentModel.DataAnnotations;

namespace CareerCracker.Models;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string OldPassword { get; set; } = "";

    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirm new password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match")]
    public string ConfirmNewPassword { get; set; } = "";
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
    public string? Experience { get; set; }
    public string? Specialization { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? Subject { get; set; }
    /// <summary>ISO date string; optional.</summary>
    public string? DateOfBirth { get; set; }
    public decimal? Salary { get; set; }
}
