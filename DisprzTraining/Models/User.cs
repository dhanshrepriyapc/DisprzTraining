using System.ComponentModel.DataAnnotations;

namespace DisprzTraining.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!; // store hashed password

        // Optional: list of appointments for this user
        public List<Appointment> Appointments { get; set; } = new();
    }
}
