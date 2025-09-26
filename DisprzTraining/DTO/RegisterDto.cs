namespace DisprzTraining.DTOs
{
    public class RegisterDto
    {
        public string Username { get; set; } = null!;
         public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string TimeZoneId { get; set; } = null!; 
    }
}
