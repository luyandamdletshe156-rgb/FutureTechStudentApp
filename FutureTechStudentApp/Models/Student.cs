using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FutureTechStudentApp.Models
{
    /// <summary>
    /// Represents a Student entity stored in Azure Cosmos DB.
    /// Profile images are stored in Azure Blob Storage with the URL referenced here.
    /// </summary>
    public class Student
    {
        /// <summary>
        /// Unique identifier for the student record.
        /// Cosmos DB requires the property name to be exactly 'id'.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Please enter the student's first name.")]
        [Display(Name = "First Name")]
        [JsonProperty("firstName")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter the student's last name.")]
        [Display(Name = "Last Name")]
        [JsonProperty("lastName")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "A valid email address is required.")]
        [EmailAddress(ErrorMessage = "That email address does not look right.")]
        [Display(Name = "Email Address")]
        [JsonProperty("email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Please provide a contact phone number.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        [JsonProperty("mobileNumber")]
        public required string MobileNumber { get; set; }

        /// <summary>
        /// Current status: "Active" or "Inactive".
        /// Used for Soft Deletion as per assignment requirements.
        /// </summary>
        [JsonProperty("enrolmentStatus")]
        public string EnrolmentStatus { get; set; } = "Active";

        /// <summary>
        /// The secure SAS URL for the student's profile picture in Azure Blob Storage.
        /// </summary>
        [JsonProperty("profileImageUrl")]
        public string? ProfileImageUrl { get; set; }
    }
}