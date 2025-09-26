using DisprzTraining.Models;
using Microsoft.EntityFrameworkCore;

namespace DisprzTraining.DataAccess
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AppDbContext _context;

        public AppointmentRepository(AppDbContext context)
        {
            _context = context;
        }

        // GET all appointments (optional)
        public async Task<List<Appointment>> GetAllAsync()
        {
            return await _context.Appointments
                                 .Include(a => a.User)
                                 .ToListAsync();
        }

        // GET appointment by ID
        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments
                                 .Include(a => a.User)
                                 .FirstOrDefaultAsync(a => a.Id == id);
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

        // SEARCH appointments by keyword (title, description, location, attendees, username)
        public async Task<List<Appointment>> SearchAsync(string keyword, int? userId)
        {
            var query = _context.Appointments
                                .Include(a => a.User)
                                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            keyword = keyword.ToLower();

            return await query
                .Where(a =>
                    a.Title.ToLower().Contains(keyword) ||
                    (a.Description != null && a.Description.ToLower().Contains(keyword)) ||
                    (a.Location != null && a.Location.ToLower().Contains(keyword)) ||
                    (a.Attendees != null && a.Attendees.ToLower().Contains(keyword)) ||
                    (a.User != null && a.User.Username.ToLower().Contains(keyword))
                )
                .ToListAsync();
        }

        // GET user details by ID
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }
    }
}
