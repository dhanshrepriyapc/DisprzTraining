
namespace DisprzTraining.DTOs
{
    public class AppointmentDto
    {
        public enum RecurrenceType
        {
            None,
            Daily,
            Weekly,
            Monthly
        }
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime StartTime { get; set; }  // exposed in user's local time
        public DateTime EndTime { get; set; }    // exposed in user's local time
        public int UserId { get; set; }

        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Attendees { get; set; }

        public string? Type { get; set; }
        public string? ColorCode { get; set; }

        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
    }
}