using DisprzTraining.Business;
using DisprzTraining.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DisprzTraining.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _service;
        private static readonly TimeZoneInfo IST = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public AppointmentsController(IAppointmentService service)
        {
            _service = service;
        }

        // Helper to get UserId from JWT claims
        private int GetUserId() => int.Parse(User.FindFirstValue("id")!);

        // Helper to normalize to IST
        private DateTime ToIst(DateTime dt) =>
            TimeZoneInfo.ConvertTimeFromUtc(dt.ToUniversalTime(), IST);

        // GET: api/appointments/user
        [HttpGet("user")]
        public async Task<IActionResult> GetUserAppointments()
        {
            var userId = GetUserId();
            var appointments = await _service.GetAppointmentsForUserAsync(userId);

            // Convert all times to IST before returning
            foreach (var appt in appointments)
            {
                appt.StartTime = ToIst(appt.StartTime);
                appt.EndTime = ToIst(appt.EndTime);
            }

            return Ok(appointments);
        }

        // POST: api/appointments/user
        [HttpPost("user")]
        public async Task<IActionResult> CreateUserAppointment([FromBody] AppointmentDto dto)
        {
            var userId = GetUserId();

            // dto.StartTime = ToIst(dto.StartTime);
            // dto.EndTime = ToIst(dto.EndTime);

            if (dto.StartTime >= dto.EndTime)
                return BadRequest(new { message = "StartTime must be before EndTime" });

            var (success, error, appointment) = await _service.CreateAppointmentAsync(dto, userId);
            if (!success) return Conflict(new { message = error });

            appointment.StartTime = ToIst(appointment.StartTime);
            appointment.EndTime = ToIst(appointment.EndTime);

            var newdto = new AppointmentDto
                {
                    Id = appointment.Id,
                    Title = appointment.Title,
                    StartTime = appointment.StartTime,
                    EndTime = appointment.EndTime,
                    Type = appointment.Type,
                    ColorCode = appointment.ColorCode
                };

return CreatedAtAction(nameof(GetUserAppointmentById), new { id = newdto.Id }, newdto);

        }

        // GET: api/appointments/user/5
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserAppointmentById(int id)
        {
            var userId = GetUserId();
            var appointment = await _service.GetAppointmentByIdAsync(id);

            if (appointment == null || appointment.UserId != userId)
                return NotFound(new { message = "Appointment not found" });

            appointment.StartTime = ToIst(appointment.StartTime);
            appointment.EndTime = ToIst(appointment.EndTime);

            var dto = new AppointmentDto
            {
                Id = appointment.Id,
                Title = appointment.Title,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                Type = appointment.Type,
                ColorCode = appointment.ColorCode
            };

            return Ok(dto);
        }

        // PUT: api/appointments/user/5
        [HttpPut("user/{id}")]
        public async Task<IActionResult> UpdateUserAppointment(int id, [FromBody] AppointmentDto dto)
        {
            var userId = GetUserId();

            dto.StartTime = ToIst(dto.StartTime);
            dto.EndTime = ToIst(dto.EndTime);

            if (dto.StartTime >= dto.EndTime)
                return BadRequest(new { message = "StartTime must be before EndTime" });

            var (success, error) = await _service.UpdateAppointmentAsync(id, dto, userId);
            if (!success)
            {
                if (error == "Not found") return NotFound(new { message = "Appointment not found" });
                if (error == "Unauthorized") return Unauthorized(new { message = "Cannot edit another user's appointment" });
                return Conflict(new { message = error });
            }

            return Ok(new { message = "Appointment updated successfully" });
        }

        // DELETE: api/appointments/user/5
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUserAppointment(int id)
        {
            var userId = GetUserId();
            var (success, error) = await _service.DeleteAppointmentAsync(id, userId);

            if (!success)
            {
                if (error == "Not found") return NotFound(new { message = "Appointment not found" });
                if (error == "Unauthorized") return Unauthorized(new { message = "Cannot delete another user's appointment" });
            }

            return NoContent();
        }

        // GET: api/appointments/user/search?keyword=meeting
        [HttpGet("user/search")]
        public async Task<IActionResult> SearchUserAppointments([FromQuery] string keyword)
        {
            var userId = GetUserId();
            var results = await _service.SearchAppointmentsAsync(keyword, userId);

            foreach (var appt in results)
            {
                appt.StartTime = ToIst(appt.StartTime);
                appt.EndTime = ToIst(appt.EndTime);
            }

            return Ok(results);
        }

        // PUT: api/appointments/user/5/type
        [HttpPut("user/{id}/type")]
        public async Task<IActionResult> UpdateTypeAndColor(int id, [FromBody] AppointmentDto dto)
        {
            var userId = GetUserId();
            var (success, error) = await _service.UpdateAppointmentTypeAsync(id, dto.Type, dto.ColorCode, userId);

            if (!success)
            {
                if (error == "Not found") return NotFound(new { message = "Appointment not found" });
                if (error == "Unauthorized") return Unauthorized(new { message = "Cannot edit another user's appointment" });
                return Conflict(new { message = error });
            }

            return Ok(new { message = "Appointment type/color updated successfully" });
        }

        // GET: api/appointments/user/recurring?start=2025-09-20&end=2025-09-27
        [HttpGet("user/recurring")]
        public async Task<IActionResult> GetUserRecurringAppointments([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (start > end)
                return BadRequest(new { message = "Start date must be before end date" });

            var userId = GetUserId();
            var appointments = await _service.GetRecurringAppointmentsAsync(userId, start, end);

            foreach (var appt in appointments)
            {
                appt.StartTime = ToIst(appt.StartTime);
                appt.EndTime = ToIst(appt.EndTime);
            }

            return Ok(appointments);
        }
    }
}
