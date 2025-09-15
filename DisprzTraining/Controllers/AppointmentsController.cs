using DisprzTraining.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisprzTraining.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/appointments
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Appointment>))]
        public async Task<IActionResult> GetAppointments()
        {
            var appointments = await _context.Appointments.ToListAsync();
            return Ok(appointments);
        }

        // ✅ GET: api/appointments/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Appointment))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentById(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound(); // 404 if not found

            return Ok(appointment); // 200 with the found entity
        }

        // ✅ POST: api/appointments
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAppointment([FromBody] Appointment appointment)
        {
            // Conflict check before saving
            bool conflict = await _context.Appointments.AnyAsync(a =>
                appointment.StartTime < a.EndTime &&
                appointment.EndTime > a.StartTime
            );

            if (conflict)
            {
                return Conflict(new { message = "Appointment time conflicts with an existing one." });
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, appointment);
        }

        // ✅ PUT: api/appointments/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] Appointment updatedAppointment)
        {
            var existing = await _context.Appointments.FindAsync(id);
            if (existing == null)
                return NotFound(); // 404 if not found

            // Check for time conflicts with other appointments
            bool conflict = await _context.Appointments.AnyAsync(a =>
                a.Id != id &&
                updatedAppointment.StartTime < a.EndTime &&
                updatedAppointment.EndTime > a.StartTime
            );

            if (conflict)
                return Conflict(new { message = "Updated appointment conflicts with an existing one." });

            // Update fields
            existing.Title = updatedAppointment.Title;
            existing.StartTime = updatedAppointment.StartTime;
            existing.EndTime = updatedAppointment.EndTime;

            await _context.SaveChangesAsync();

            return Ok(existing); // 200 OK
        }

        // ✅ DELETE: api/appointments/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}
