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

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var result = await _service.GetAppointmentsAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDto dto)
        {
            var (success, error, appointment) = await _service.CreateAppointmentAsync(dto);

            if (!success)
                return Conflict(new { message = error });

            return CreatedAtAction(
                nameof(GetById),       // the GET endpoint
                new { id = appointment.Id },     // route values
                appointment                       // response body
            );
        }

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
                EndTime = appointment.EndTime
            };
            return Ok(appointment);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentDto dto)
        {
            var (success, error) = await _service.UpdateAppointmentAsync(id, dto);
            if (!success)
            {
                if (error == "Not found") return NotFound();
                return Conflict(new { message = error });
            }

            return Ok(new { message = "Appointment updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var success = await _service.DeleteAppointmentAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
    }
}
