using DisprzTraining.Business;
using DisprzTraining.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DisprzTraining.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly IConfiguration _configuration;

        public UsersController(IUserService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _service.AuthenticateAsync(dto.Username, dto.Password);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });
            
            // Use the UserService method instead of controller method
            var token = _service.GenerateJwtToken(user);
            
            return Ok(new
            {
                User = new UserDto  
                {
                    Id = user.Id,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    TimeZoneId = user.TimeZoneId
                },
                Token = token
            });
        }

        // Register user - api/users/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                // Force IST if not provided
                var tzId = string.IsNullOrEmpty(dto.TimeZoneId) 
                    ? "Asia/Calcutta" 
                    : dto.TimeZoneId;

                var user = await _service.RegisterAsync(dto.Username, dto.FirstName,dto.LastName, dto.Password, tzId);

                return CreatedAtAction(nameof(Register), 
                    new { id = user.Id }, 
                    new UserDto { Id = user.Id, Username = user.Username, FirstName= user.FirstName, LastName= user.LastName });
            }
            catch (Exception ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
