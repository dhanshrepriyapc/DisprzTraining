namespace DisprzTraining.DTOs
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }  
        public DateTime StartTime { get; set; }  
        public DateTime EndTime { get; set; }  
    }
}
