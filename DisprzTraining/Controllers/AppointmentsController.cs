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

        public AppointmentsController(IAppointmentService service)
        {
            _service = service;
        }

        // Helper: get user ID from JWT
        private int GetUserId() => int.Parse(User.FindFirstValue("id")!);

        // Helper: get user's TimeZoneId from JWT (or user profile)
        private string GetUserTimeZoneId() 
            {
                var timeZoneId = User.FindFirstValue("timeZoneId") ?? "UTC";
                Console.WriteLine($"GetUserTimeZoneId: {timeZoneId}");
                return timeZoneId;
            }

        // Convert UTC to user's local time
        private DateTime ToUserTime(DateTime utcTime)
        {
            var timeZoneId = GetUserTimeZoneId();
            Console.WriteLine($"Converting UTC {utcTime} to timezone {timeZoneId}");
            
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var result = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
            
            Console.WriteLine($"Result: {result}");
            return result;
        }

        // Convert user's local time to UTC
        private DateTime ToUtc(DateTime userTime)
        {
            var timeZoneId = GetUserTimeZoneId();
            Console.WriteLine($"Converting local time {userTime} from timezone {timeZoneId} to UTC");
            
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var result = TimeZoneInfo.ConvertTimeToUtc(userTime, tz);
            
            Console.WriteLine($"UTC Result: {result}");
            return result;
        }

        // GET: api/appointments/user
        [HttpGet("user")]
        public async Task<IActionResult> GetUserAppointments()
        {
            var userId = GetUserId();
            var appointments = await _service.GetAppointmentsForUserAsync(userId);

            // Convert each appointment to user time
            appointments.ForEach(a =>
            {
                a.StartTime = ToUserTime(a.StartTime);
                a.EndTime = ToUserTime(a.EndTime);
            });

            return Ok(appointments);
        }

        // POST: api/appointments/user
        [HttpPost("user")]
        public async Task<IActionResult> CreateUserAppointment([FromBody] AppointmentDto dto)
        {
            var userId = GetUserId();

            if (dto.StartTime >= dto.EndTime)
                return BadRequest(new { message = "StartTime must be before EndTime" });

            // Convert input times to UTC
            dto.StartTime = ToUtc(dto.StartTime);
            dto.EndTime = ToUtc(dto.EndTime);

            var (success, error, appointment) = await _service.CreateAppointmentAsync(dto, userId);
            if (!success) return Conflict(new { message = error });
            
            if (appointment == null)
                return StatusCode(500, new { message = "Failed to create appointment" });

            // Convert times back to user local
            var newdto = new AppointmentDto
            {
                Id = appointment.Id,
                Title = appointment.Title,
                StartTime = ToUserTime(appointment.StartTime),
                EndTime = ToUserTime(appointment.EndTime),
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

            var dto = new AppointmentDto
            {
                Id = appointment.Id,
                Title = appointment.Title,
                StartTime = ToUserTime(appointment.StartTime),
                EndTime = ToUserTime(appointment.EndTime),
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

            if (dto.StartTime >= dto.EndTime)
                return BadRequest(new { message = "StartTime must be before EndTime" });

            // Convert to UTC
            dto.StartTime = ToUtc(dto.StartTime);
            dto.EndTime = ToUtc(dto.EndTime);

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

            // Convert to user local time
            results.ForEach(a =>
            {
                a.StartTime = ToUserTime(a.StartTime);
                a.EndTime = ToUserTime(a.EndTime);
            });

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

            // Convert query params from user local → UTC
            start = ToUtc(start);
            end = ToUtc(end);

            var userId = GetUserId();
            var appointments = await _service.GetRecurringAppointmentsAsync(userId, start, end);

            // Convert back to user local
            appointments.ForEach(a =>
            {
                a.StartTime = ToUserTime(a.StartTime);
                a.EndTime = ToUserTime(a.EndTime);
            });

            return Ok(appointments);
        }
    }
}
