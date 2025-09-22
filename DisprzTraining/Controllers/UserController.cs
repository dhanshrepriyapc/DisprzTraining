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
        
        // Login user - api/users/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _service.AuthenticateAsync(dto.Username, dto.Password);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                User = new UserDto 
                { 
                    Id = user.Id, 
                    Username = user.Username 
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
                    ? "India Standard Time" 
                    : dto.TimeZoneId;

                var user = await _service.RegisterAsync(dto.Username, dto.Password, tzId);

                return CreatedAtAction(nameof(Register), 
                    new { id = user.Id }, 
                    new UserDto { Id = user.Id, Username = user.Username });
            }
            catch (Exception ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // Private helper to generate JWT token
        private string GenerateJwtToken(Models.User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] 
                ?? "ThisIsASuperSecureLongJwtKeyForHS25612345!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("id", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "MyApp",
                audience: _configuration["Jwt:Audience"] ?? "MyApp",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
