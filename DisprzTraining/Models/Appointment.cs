namespace DisprzTraining.Models
{
    public enum RecurrenceType
    {
        None,
        Daily,
        Weekly,
        Monthly
    }

    public class Appointment
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        // User relationship
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Attendees { get; set; }
        public string? Type { get; set; }
        public string? ColorCode { get; set; }

        // Recurrence
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }

       
    }
}
