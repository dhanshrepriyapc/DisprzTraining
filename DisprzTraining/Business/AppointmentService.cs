using DisprzTraining.DataAccess;
using DisprzTraining.DTOs;
using DisprzTraining.Models;
using Microsoft.EntityFrameworkCore;

namespace DisprzTraining.Business
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
         private readonly IUserRepository _userRepository;

        public AppointmentService(IAppointmentRepository repository, IUserRepository userRepository)
        {
            _repository = repository;
            _userRepository = userRepository; 
        }

        // GET all appointments for a specific user
        public async Task<List<AppointmentDto>> GetAppointmentsForUserAsync(int userId)
        {
            var appointments = await _repository.GetByUserIdAsync(userId);
            return appointments.Select(MapToDto).ToList();
        }

        // GET appointment by ID
        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // Helper to safely add months
        private DateTime AddMonthsSafely(DateTime dt, int months)
        {
            int targetMonth = dt.Month + months;
            int targetYear = dt.Year + (targetMonth - 1) / 12;
            targetMonth = ((targetMonth - 1) % 12) + 1;
            int day = Math.Min(dt.Day, DateTime.DaysInMonth(targetYear, targetMonth));
            return new DateTime(targetYear, targetMonth, day, dt.Hour, dt.Minute, dt.Second);
        }

        // CREATE new appointment for a specific user (materialized recurring appointments)
        public async Task<(bool Success, string? Error, Appointment? Appointment)> CreateAppointmentAsync(AppointmentDto dto, int userId)
        {
            try
            {
                Console.WriteLine($"=== CREATE APPOINTMENT DEBUG ===");
                Console.WriteLine($"DTO Recurrence: {dto.Recurrence}");
                Console.WriteLine($"DTO RecurrenceInterval: {dto.RecurrenceInterval}");
                Console.WriteLine($"DTO RecurrenceEndDate: {dto.RecurrenceEndDate}");
                Console.WriteLine($"Start Time: {dto.StartTime}");
                Console.WriteLine($"End Time: {dto.EndTime}");

                // Get user to access their timezone
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return (false, "User not found", null);

                // Validate against past bookings in user's timezone
                var validationResult = ValidateAppointmentTime(dto.StartTime, dto.EndTime, user.TimeZoneId);
                if (!validationResult.IsValid)
                    return (false, validationResult.ErrorMessage, null);

                var start = dto.StartTime;
                var end = dto.EndTime;

                if (start >= end)
                    return (false, "StartTime must be before EndTime", null);

                var existingAppointments = await _repository.GetByUserIdAsync(userId);
                bool Overlaps(DateTime s1, DateTime e1, DateTime s2, DateTime e2) => s1 < e2 && e1 > s2;

                var createdAppointments = new List<Appointment>();

                if (dto.Recurrence != AppointmentDto.RecurrenceType.None)
                {
                    var occurrence = start;
                    var recurrenceEnd = dto.RecurrenceEndDate ?? start.AddMonths(3);
                    var interval = dto.RecurrenceInterval ?? 1;

                    Console.WriteLine($"Creating recurring appointments from {occurrence} until {recurrenceEnd} with interval {interval}");

                    int maxOccurrences = 100; // Safety limit
                    int createdCount = 0;

                    while (occurrence.Date <= recurrenceEnd.Date && createdCount < maxOccurrences)
                    {
                        var occurrenceEnd = occurrence + (end - start);

                        Console.WriteLine($"Creating occurrence #{createdCount + 1}: {occurrence} to {occurrenceEnd}");

                        // Validate each recurring appointment against past time
                        var recurringValidation = ValidateAppointmentTime(occurrence, occurrenceEnd, user.TimeZoneId);
                        if (!recurringValidation.IsValid)
                        {
                            // Skip past occurrences for recurring appointments, but continue with future ones
                            Console.WriteLine($"Skipping past occurrence: {occurrence}");
                            
                            // Move to next occurrence
                            var nextOccurrence = dto.Recurrence switch
                            {
                                AppointmentDto.RecurrenceType.Daily => occurrence.AddDays(interval),
                                AppointmentDto.RecurrenceType.Weekly => occurrence.AddDays(7 * interval),
                                AppointmentDto.RecurrenceType.Monthly => AddMonthsSafely(occurrence, interval),
                                _ => recurrenceEnd.AddDays(1)
                            };
                            
                            if (nextOccurrence <= occurrence)
                                break;
                            
                            occurrence = nextOccurrence;
                            continue;
                        }

                        // Check overlap with existing appointments
                        foreach (var existing in existingAppointments)
                        {
                            if (Overlaps(occurrence, occurrenceEnd, existing.StartTime, existing.EndTime))
                                return (false, $"Recurring appointment on {occurrence:yyyy-MM-dd} overlaps with existing appointment", null);
                        }

                        // Create appointment
                        var appointment = new Appointment
                        {
                            Title = dto.Title ?? string.Empty, 
                            StartTime = occurrence,
                            EndTime = occurrenceEnd,
                            UserId = userId,
                            Description = dto.Description,
                            Location = dto.Location,
                            Attendees = dto.Attendees,
                            Type = dto.Type,
                            ColorCode = dto.ColorCode,
                            Recurrence = RecurrenceType.None,
                            RecurrenceInterval = null,
                            RecurrenceEndDate = null
                        };

                        await _repository.AddAsync(appointment);
                        createdAppointments.Add(appointment);
                        createdCount++;

                        // Move to next occurrence
                        var nextOccurrence2 = dto.Recurrence switch
                        {
                            AppointmentDto.RecurrenceType.Daily => occurrence.AddDays(interval),
                            AppointmentDto.RecurrenceType.Weekly => occurrence.AddDays(7 * interval),
                            AppointmentDto.RecurrenceType.Monthly => AddMonthsSafely(occurrence, interval),
                            _ => recurrenceEnd.AddDays(1)
                        };

                        if (nextOccurrence2 <= occurrence)
                        {
                            Console.WriteLine("Breaking: Next occurrence is not after current");
                            break;
                        }
                        occurrence = nextOccurrence2;
                    }

                    Console.WriteLine($"Created {createdAppointments.Count} recurring appointments");
                    if (createdAppointments.Count == 0)
                        return (false, "No future appointments could be created. All occurrences are in the past.", null);

                    return (true, null, createdAppointments.First());
                }
                else
                {
                    // Single appointment
                    if (existingAppointments.Any(a => Overlaps(start, end, a.StartTime, a.EndTime)))
                        return (false, "Appointment time overlaps with existing appointment", null);

                    var appointment = new Appointment
                    {
                        Title = dto.Title ?? string.Empty, 
                        StartTime = start,
                        EndTime = end,
                        UserId = userId,
                        Description = dto.Description,
                        Location = dto.Location,
                        Attendees = dto.Attendees,
                        Type = dto.Type,
                        ColorCode = dto.ColorCode,
                        Recurrence = RecurrenceType.None
                    };

                    await _repository.AddAsync(appointment);
                    return (true, null, appointment);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating appointment: {ex.Message}");
                return (false, ex.Message, null);
            }
        }
        private (bool IsValid, string? ErrorMessage) ValidateAppointmentTime(DateTime startTime, DateTime endTime, string userTimeZoneId)
        {
            try
            {
                // Get user's timezone
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
                
                // Convert UTC times to user's local time for validation
                var userLocalStartTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, userTimeZone);
                var userLocalEndTime = TimeZoneInfo.ConvertTimeFromUtc(endTime, userTimeZone);
                var userCurrentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

                Console.WriteLine($"Validation - User TZ: {userTimeZoneId}");
                Console.WriteLine($"Validation - Current time in user TZ: {userCurrentTime}");
                Console.WriteLine($"Validation - Appointment start in user TZ: {userLocalStartTime}");
                Console.WriteLine($"Validation - Appointment end in user TZ: {userLocalEndTime}");

                // Check if appointment start time is in the past (in user's timezone)
                if (userLocalStartTime <= userCurrentTime)
                {
                    return (false, $"Cannot book appointments in the past. Current time in your timezone ({userTimeZoneId}): {userCurrentTime:yyyy-MM-dd HH:mm}");
                }

                // Check if appointment end time is in the past (in user's timezone)
                if (userLocalEndTime <= userCurrentTime)
                {
                    return (false, $"Appointment end time cannot be in the past. Current time in your timezone ({userTimeZoneId}): {userCurrentTime:yyyy-MM-dd HH:mm}");
                }

                return (true, null);
            }
            catch (TimeZoneNotFoundException)
            {
                return (false, "Invalid timezone configuration for user");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating appointment time: {ex.Message}");
                return (false, "Error validating appointment time");
            }
        }

        // UPDATE appointment
        public async Task<(bool Success, string? Error)> UpdateAppointmentAsync(int id, AppointmentDto dto, int userId)
        {
            var appointment = await _repository.GetByIdAsync(id);
            if (appointment == null) return (false, "Not found");
            if (appointment.UserId != userId) return (false, "Unauthorized");

            var start = dto.StartTime;
            var end = dto.EndTime;

            if (start >= end)
                return (false, "StartTime must be before EndTime");

            var userAppointments = await _repository.GetByUserIdAsync(userId);
            if (userAppointments.Any(a => a.Id != id && start < a.EndTime && end > a.StartTime))
                return (false, "Appointment time overlaps with existing appointment");

            appointment.Title = dto.Title?? appointment.Title;
            appointment.StartTime = start;
            appointment.EndTime = end;
            appointment.Description = dto.Description;
            appointment.Location = dto.Location;
            appointment.Attendees = dto.Attendees;
            appointment.Type = dto.Type;
            appointment.ColorCode = dto.ColorCode;
            appointment.Recurrence = (RecurrenceType)dto.Recurrence;
            appointment.RecurrenceInterval = dto.RecurrenceInterval;
            appointment.RecurrenceEndDate = dto.RecurrenceEndDate;

            await _repository.UpdateAsync(appointment);
            return (true, null);
        }

        // DELETE appointment
        public async Task<(bool Success, string? Error)> DeleteAppointmentAsync(int id, int userId)
        {
            var appointment = await _repository.GetByIdAsync(id);
            if (appointment == null) return (false, "Not found");
            if (appointment.UserId != userId) return (false, "Unauthorized");

            await _repository.DeleteAsync(appointment);
            return (true, null);
        }

        // SEARCH appointments
        public async Task<List<AppointmentDto>> SearchAppointmentsAsync(string keyword, int? userId)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<AppointmentDto>();

            var results = await _repository.SearchAsync(keyword, userId);
            return results.Select(MapToDto).ToList();
        }

        // UPDATE appointment type/color
        public async Task<(bool Success, string? Error)> UpdateAppointmentTypeAsync(int id, string? type, string? colorCode, int userId)
        {
            var appointment = await _repository.GetByIdAsync(id);
            if (appointment == null) return (false, "Not found");
            if (appointment.UserId != userId) return (false, "Unauthorized");

            appointment.Type = type;
            appointment.ColorCode = colorCode;

            await _repository.UpdateAsync(appointment);
            return (true, null);
        }

        // GET recurring appointments (simplified for materialized occurrences)
        public async Task<List<AppointmentDto>> GetRecurringAppointmentsAsync(int userId, DateTime start, DateTime end)
        {
            var appointments = await _repository.GetByUserIdAsync(userId);
            return appointments
                .Where(a => a.StartTime <= end && a.EndTime >= start)
                .Select(MapToDto)
                .OrderBy(a => a.StartTime)
                .ToList();
        }

        // Map Appointment entity to DTO
        private AppointmentDto MapToDto(Appointment a) => new AppointmentDto
        {
            Id = a.Id,
            Title = a.Title,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            UserId = a.UserId,
            Description = a.Description,
            Location = a.Location,
            Attendees = a.Attendees,
            Type = a.Type,
            ColorCode = a.ColorCode,
            Recurrence = (AppointmentDto.RecurrenceType)a.Recurrence,
            RecurrenceInterval = a.RecurrenceInterval,
            RecurrenceEndDate = a.RecurrenceEndDate
        };
    }
}
