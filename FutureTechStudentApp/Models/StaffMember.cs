using System.ComponentModel.DataAnnotations;

namespace FutureTechStudentApp.Models
{
    public class StaffMember
    {
        public string? StaffNumber { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        public required string FullName { get; set; } // REMOVED 'required' keyword

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Please confirm your email")]
        [Compare("Email", ErrorMessage = "Emails do not match")]
        public required string ConfirmEmail { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public required string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Please select a department")]
        public required string Department { get; set; }

        public string? Role { get; set; }
    }
}