using System.ComponentModel.DataAnnotations;

namespace SleazyRetailers.Models
{
    public class User
    {
        [Key]
        public string? Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } = "Customer"; // Customer or Admin

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}