using DisprzTraining.DataAccess;
using DisprzTraining.DTOs;
using DisprzTraining.Models;

namespace DisprzTraining.Business
{
    public class AppointmentService
    {
        private readonly AppointmentRepository _repository;

        //AppointmentRepository is injected → 
        // the service doesn’t talk to DB directly, it uses the repo.

        public AppointmentService(AppointmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Appointment>> GetAppointmentsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }
        public async Task<(bool Success, string? Error, Appointment? Created)> CreateAppointmentAsync(AppointmentDto dto)
        {
            var existing = await _repository.GetAllAsync();
            bool conflict = existing.Any(a =>
                dto.StartTime < a.EndTime && dto.EndTime > a.StartTime);

            if (conflict)
                return (false, "Appointment time conflicts with an existing one.", null);

            var appointment = new Appointment
            {
                Title = dto.Title,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime
            };

            await _repository.AddAsync(appointment);

            return (true, null, appointment);
        }

        // public async Task<(bool Success, string? Error, Appointment? Created)> CreateAppointmentAsync(AppointmentDto dto)
        //     {
        //         var existing = await _repository.GetAllAsync();
        //         bool conflict = existing.Any(a =>
        //             dto.StartTime < a.EndTime && dto.EndTime > a.StartTime);

        //         if (conflict)
        //             return (false, "Appointment time conflicts with an existing one.", null);

        //         var appointment = new Appointment
        //         {
        //             Title = dto.Title,
        //             StartTime = dto.StartTime,
        //             EndTime = dto.EndTime
        //         };

        //         await _repository.AddAsync(appointment);

        //         return (true, null, appointment);
        //     }

        public async Task<(bool Success, string? Error)> UpdateAppointmentAsync(int id, AppointmentDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return (false, "Not found");

            var all = await _repository.GetAllAsync();
            bool conflict = all.Any(a =>
                a.Id != id && dto.StartTime < a.EndTime && dto.EndTime > a.StartTime);

            if (conflict) return (false, "Conflicts with another appointment.");

            existing.Title = dto.Title;
            existing.StartTime = dto.StartTime;
            existing.EndTime = dto.EndTime;

            await _repository.UpdateAsync(existing);
            return (true, null);
        }

        public async Task<bool> DeleteAppointmentAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            await _repository.DeleteAsync(existing);
            return true;
        }
    }
}
