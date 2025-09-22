namespace DisprzTraining.DTOs
{
    public class RegisterDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? TimeZoneId { get; set; } = "India Standard Time"; // default IST
    }
}
