using DisprzTraining.Business;
using DisprzTraining.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DisprzTraining.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _service;

        public UsersController(UserService service)
        {
            _service = service;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _service.AuthenticateAsync(dto.Username, dto.Password);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            return Ok(new UserDto { Id = user.Id, Username = user.Username });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _service.RegisterAsync(dto.Username, dto.Password);
                return CreatedAtAction(nameof(Register), new { id = user.Id }, new UserDto { Id = user.Id, Username = user.Username });
            }
            catch (Exception ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
