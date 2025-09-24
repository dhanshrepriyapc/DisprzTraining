using DisprzTraining.DataAccess;
using DisprzTraining.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace DisprzTraining.Business
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IConfiguration _config;

        public UserService(IUserRepository repository, IConfiguration config)
        {
            _repository = repository;
            _config = config;
        }

        // Authenticate user
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _repository.GetByUsernameAsync(username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Verify hashed password
             return user;
        }
        
// Generate JWT token for authenticated user
        public string GenerateJwtToken(User user)
        {
            Console.WriteLine($"Generating JWT for user: {user.Username}, TimeZone: {user.TimeZoneId}");

            var secretKey = _config["Jwt:Key"] ?? "ThisIsAReallyLongSuperSecretKey123!";
            if (secretKey.Length < 32)
                secretKey = secretKey.PadRight(32, 'X');

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("id", user.Id.ToString()),
                new Claim("timeZoneId", user.TimeZoneId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            Console.WriteLine($"JWT Claims: {string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}"))}");

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "MyApp",
                audience: _config["Jwt:Audience"] ?? "MyApp",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"Generated JWT token: {tokenString}");

            return tokenString;
        }


        // Register new user
        public async Task<User> RegisterAsync(string username, string firstName, string lastName, string password, string? timeZoneId = null)
        {
            var existing = await _repository.GetByUsernameAsync(username);
            if (existing != null) throw new Exception("Username already exists");

            var user = new User
            {
                Username = username,
                // Hash password before storing
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                // PasswordHash = password,
                TimeZoneId = timeZoneId ?? TimeZoneInfo.Local.Id,
                FirstName = firstName,
                LastName = lastName
            };

            await _repository.AddAsync(user);
            return user;
        }

        // Update user's time zone
        public async Task<(bool Success, string? Error)> UpdateTimeZoneAsync(int userId, string timeZoneId)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null) return (false, "User not found");

            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                user.TimeZoneId = timeZoneId;
                await _repository.UpdateAsync(user);
                return (true, null);
            }
            catch (TimeZoneNotFoundException)
            {
                return (false, "Invalid time zone ID");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
