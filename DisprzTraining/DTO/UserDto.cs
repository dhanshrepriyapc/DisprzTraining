namespace DisprzTraining.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        // Needed so appointments can be converted properly
        public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
    }
}
