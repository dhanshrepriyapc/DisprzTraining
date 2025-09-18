using DisprzTraining.DTOs;
using DisprzTraining.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DisprzTraining.Business
{
    public interface IAppointmentService
    {
        Task<List<AppointmentDto>> GetAppointmentsForUserAsync(int userId);
        Task<Appointment?> GetAppointmentByIdAsync(int id);
        Task<(bool Success, string? Error, Appointment Appointment)> CreateAppointmentAsync(AppointmentDto dto, int userId);
        Task<(bool Success, string? Error)> UpdateAppointmentAsync(int id, AppointmentDto dto, int userId);
        Task<(bool Success, string? Error)> DeleteAppointmentAsync(int id, int userId);
    }
}
