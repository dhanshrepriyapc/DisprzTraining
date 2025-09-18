using DisprzTraining.Models;
using Microsoft.EntityFrameworkCore;

namespace DisprzTraining.DataAccess
{
    public class AppointmentRepository
    {
        private readonly AppDbContext _context;

        public AppointmentRepository(AppDbContext context)
        {
            _context = context;
        }

        // GET all appointments (optional)
        public async Task<List<Appointment>> GetAllAsync()
        {
            return await _context.Appointments.ToListAsync();
        }

        // GET appointment by ID
        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments.FindAsync(id);
        }

        // GET all appointments for a specific user
        public async Task<List<Appointment>> GetByUserIdAsync(int userId)
        {
            return await _context.Appointments
                                 .Where(a => a.UserId == userId)
                                 .OrderBy(a => a.StartTime)
                                 .ToListAsync();
        }

        // ADD new appointment
        public async Task AddAsync(Appointment appointment)
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
        }

        // UPDATE existing appointment
        public async Task UpdateAsync(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
        }

        // DELETE appointment
        public async Task DeleteAsync(Appointment appointment)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
    }
}

