using DisprzTraining.Business;
using DisprzTraining.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DisprzTraining.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentService _service;
        // builder.Services.AddScoped<AppointmentService>();
        // DI injection 

        public AppointmentsController(AppointmentService service)
        {
            _service = service;
        }

        // GET: api/appointments?userId=1
        [HttpGet]
        public async Task<IActionResult> GetAppointments([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var result = await _service.GetAppointmentsForUserAsync(userId);
            return Ok(result);
        }

        // POST: api/appointments?userId=1
        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDto dto, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var (success, error, appointment) = await _service.CreateAppointmentAsync(dto, userId);

            if (!success)
                return Conflict(new { message = error });

            return CreatedAtAction(
                nameof(GetById),
                new { id = appointment.Id },
                appointment
            );
        }

        // GET: api/appointments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentDto>> GetById(int id)
        {
            var appointment = await _service.GetAppointmentByIdAsync(id);
            if (appointment == null) return NotFound();

            var dto = new AppointmentDto
            {
                Id = appointment.Id,
                Title = appointment.Title,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                UserId = appointment.UserId
            };

            return Ok(dto);
        }

        // PUT: api/appointments/5?userId=1
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentDto dto, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var (success, error) = await _service.UpdateAppointmentAsync(id, dto, userId);

            if (!success)
            {
                if (error == "Not found") return NotFound();
                if (error == "Unauthorized") return Unauthorized(new { message = "Cannot edit another user's appointment" });
                return Conflict(new { message = error });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = id },
                new { message = "Appointment updated successfully" }
            );
        }

        // DELETE: api/appointments/5?userId=1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var (success, error) = await _service.DeleteAppointmentAsync(id, userId);

            if (!success)
            {
                if (error == "Not found") return NotFound();
                if (error == "Unauthorized") return Unauthorized(new { message = "Cannot delete another user's appointment" });
            }

            return NoContent();
        }
    }
}
