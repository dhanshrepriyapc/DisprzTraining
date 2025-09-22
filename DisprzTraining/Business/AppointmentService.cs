using DisprzTraining.DataAccess;
using DisprzTraining.DTOs;
using DisprzTraining.Models;
using Microsoft.EntityFrameworkCore;

namespace DisprzTraining.Business
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private static readonly TimeZoneInfo IST = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public AppointmentService(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        // GET all appointments for a specific user
        public async Task<List<AppointmentDto>> GetAppointmentsForUserAsync(int userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            var appointments = await _repository.GetByUserIdAsync(userId);

            // If user has a specific timezone, use it; otherwise default IST
            var tzId = string.IsNullOrEmpty(user?.TimeZoneId) ? IST.Id : user.TimeZoneId;
            var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            return appointments.Select(a =>
            {
                var dto = MapToDto(a);
                dto.StartTime = TimeZoneInfo.ConvertTimeFromUtc(a.StartTime, userTimeZone);
                dto.EndTime = TimeZoneInfo.ConvertTimeFromUtc(a.EndTime, userTimeZone);
                return dto;
            }).ToList();
        }

        // GET appointment by ID
        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // CREATE new appointment for a specific user
        public async Task<(bool Success, string? Error, Appointment? Appointment)> CreateAppointmentAsync(AppointmentDto dto, int userId)
        {
            try
            {
                // Store everything in UTC internally
                var startUtc = dto.StartTime.ToUniversalTime();
                var endUtc = dto.EndTime.ToUniversalTime();

                if (startUtc < DateTime.UtcNow || endUtc < DateTime.UtcNow)
                    return (false, "Cannot book appointments in the past", null);

                var existingAppointments = await _repository.GetByUserIdAsync(userId);
                if (existingAppointments.Any(a => startUtc < a.EndTime && endUtc > a.StartTime))
                    return (false, "Appointment time overlaps with existing appointment", null);

                var appointment = new Appointment
                {
                    Title = dto.Title,
                    StartTime = startUtc,
                    EndTime = endUtc,
                    UserId = userId,
                    Description = dto.Description,
                    Location = dto.Location,
                    Attendees = dto.Attendees,
                    Type = dto.Type,
                    ColorCode = dto.ColorCode,
                    Recurrence = (RecurrenceType)dto.Recurrence,
                    RecurrenceInterval = dto.RecurrenceInterval,
                    RecurrenceEndDate = dto.RecurrenceEndDate
                };

                await _repository.AddAsync(appointment);
                return (true, null, appointment);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        // UPDATE appointment
        public async Task<(bool Success, string? Error)> UpdateAppointmentAsync(int id, AppointmentDto dto, int userId)
        {
            var appointment = await _repository.GetByIdAsync(id);
            if (appointment == null) return (false, "Not found");
            if (appointment.UserId != userId) return (false, "Unauthorized");

            var startUtc = dto.StartTime.ToUniversalTime();
            var endUtc = dto.EndTime.ToUniversalTime();

            if (startUtc < DateTime.UtcNow || endUtc < DateTime.UtcNow)
                return (false, "Cannot set appointments in the past");

            var userAppointments = await _repository.GetByUserIdAsync(userId);
            if (userAppointments.Any(a => a.Id != id && startUtc < a.EndTime && endUtc > a.StartTime))
                return (false, "Appointment time overlaps with existing appointment");

            appointment.Title = dto.Title;
            appointment.StartTime = startUtc;
            appointment.EndTime = endUtc;
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

      // GET recurring appointments
public async Task<List<AppointmentDto>> GetRecurringAppointmentsAsync(int userId, DateTime start, DateTime end)
{
    var user = await _repository.GetUserByIdAsync(userId);
    var tzId = string.IsNullOrEmpty(user?.TimeZoneId) ? IST.Id : user.TimeZoneId;
    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);

    var appointments = await _repository.GetByUserIdAsync(userId);
    var result = new List<AppointmentDto>();

    foreach (var a in appointments)
    {
        // Non-recurring appointments
        if (a.Recurrence == RecurrenceType.None)
        {
            if (a.StartTime <= end && a.EndTime >= start) // overlaps with range
            {
                var dto = MapToDto(a);
                dto.StartTime = TimeZoneInfo.ConvertTimeFromUtc(a.StartTime, userTimeZone);
                dto.EndTime = TimeZoneInfo.ConvertTimeFromUtc(a.EndTime, userTimeZone);
                result.Add(dto);
            }
            continue;
        }

        // Recurring appointments
        var occurrence = a.StartTime;
        var recurrenceEnd = a.RecurrenceEndDate ?? end; // if no end date, limit to 'end'

        while (occurrence <= recurrenceEnd)
        {
            var occurrenceEnd = occurrence + (a.EndTime - a.StartTime);

            // Only include occurrences within requested range
            if (occurrenceEnd >= start && occurrence <= end)
            {
                var dto = MapToDto(a);
                dto.StartTime = TimeZoneInfo.ConvertTimeFromUtc(occurrence, userTimeZone);
                dto.EndTime = TimeZoneInfo.ConvertTimeFromUtc(occurrenceEnd, userTimeZone);
                result.Add(dto);
            }

            // Move to next occurrence based on recurrence type
            occurrence = a.Recurrence switch
            {
                RecurrenceType.Daily => occurrence.AddDays(a.RecurrenceInterval ?? 1),
                RecurrenceType.Weekly => occurrence.AddDays(7 * (a.RecurrenceInterval ?? 1)),
                RecurrenceType.Monthly => occurrence.AddMonths(a.RecurrenceInterval ?? 1),
                _ => recurrenceEnd.AddDays(1) // safety to exit loop
            };
        }
    }

    return result.OrderBy(a => a.StartTime).ToList();
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
