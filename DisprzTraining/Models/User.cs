using System.ComponentModel.DataAnnotations;

namespace DisprzTraining.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required] 
        public string Username { get; set; } = null!;

        [Required]
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!; // store hashed password

        // Each user has a fixed timezone
        [Required]
        public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id; // default server timezone

        // Navigation property
        public List<Appointment> Appointments { get; set; } = new();
    }
}
