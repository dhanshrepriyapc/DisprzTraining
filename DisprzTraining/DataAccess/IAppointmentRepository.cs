using DisprzTraining.Models;

namespace DisprzTraining.DataAccess
{
    public interface IAppointmentRepository
    {
        Task<List<Appointment>> GetAllAsync();
        Task<Appointment?> GetByIdAsync(int id);
        Task<List<Appointment>> GetByUserIdAsync(int userId);
        Task AddAsync(Appointment appointment);
        Task UpdateAsync(Appointment appointment);
        Task DeleteAsync(Appointment appointment);
        Task<List<Appointment>> SearchAsync(string keyword, int? userId);

        // Needed for AppointmentService to fetch user info for timezone etc.
        Task<User?> GetUserByIdAsync(int userId);
    }
}
