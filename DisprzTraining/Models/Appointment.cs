namespace DisprzTraining.Models
{
  public class Appointment
  {
    public int Id { get; set; }        // Unique ID for the appointment
    public string Title { get; set; } = string.Empty;  // Name of the appointment
    public DateTime StartTime { get; set; }  // Start time of the appointment
    public DateTime EndTime { get; set; }    // End time of the appointment
                                             // Time of the appointment
      // New fields for user relationship
    public int UserId { get; set; }
    public User User { get; set; }
    }
}
