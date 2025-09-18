using DisprzTraining.DataAccess;
using DisprzTraining.DTOs;
using DisprzTraining.Models;
using Microsoft.EntityFrameworkCore;

namespace DisprzTraining.Business
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;

        public AppointmentService(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        // GET all appointments for a specific user
         public async Task<List<AppointmentDto>> GetAppointmentsForUserAsync(int userId)
        {
            var appointments = await _repository.GetByUserIdAsync(userId);

            return appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                Title = a.Title,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                UserId = a.UserId
            }).ToList();
        }

        // GET appointment by ID
         public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // CREATE new appointment for a specific user
         public async Task<(bool Success, string? Error, Appointment Appointment)> CreateAppointmentAsync(AppointmentDto dto, int userId)
        {
            try
            {
                // Optional: check for overlapping appointments for the same user
                var overlap = await _repository.GetByUserIdAsync(userId);
                if (overlap.Any(a => dto.StartTime < a.EndTime && dto.EndTime > a.StartTime))
                    return (false, "Appointment time overlaps with existing appointment", null!);

                var appointment = new Appointment
                {
                    Title = dto.Title,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    UserId = userId
                };

                await _repository.AddAsync(appointment);
                return (true, null, appointment);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null!);
            }
        }

        // UPDATE appointment (only by the owner)
         public async Task<(bool Success, string? Error)> UpdateAppointmentAsync(int id, AppointmentDto dto, int userId)
        {
            var appointment = await _repository.GetByIdAsync(id);
            if (appointment == null) return (false, "Not found");
            if (appointment.UserId != userId) return (false, "Unauthorized");

            // Optional: check overlapping
            var userAppointments = await _repository.GetByUserIdAsync(userId);
            if (userAppointments.Any(a => a.Id != id && dto.StartTime < a.EndTime && dto.EndTime > a.StartTime))
                return (false, "Appointment time overlaps with existing appointment");

            appointment.Title = dto.Title;
            appointment.StartTime = dto.StartTime;
            appointment.EndTime = dto.EndTime;

            await _repository.UpdateAsync(appointment);
            return (true, null);
        }

        // In AppointmentRepositor


        // DELETE appointment (only by the owner)
         public async Task<(bool Success, string? Error)> DeleteAppointmentAsync(int id, int userId)
        {
            var appointment = await _repository.GetByIdAsync(id);
            if (appointment == null) return (false, "Not found");
            if (appointment.UserId != userId) return (false, "Unauthorized");

            await _repository.DeleteAsync(appointment);
            return (true, null);
        }
    }
}
