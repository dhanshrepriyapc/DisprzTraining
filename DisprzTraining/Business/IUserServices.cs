using DisprzTraining.DataAccess;
using DisprzTraining.Models;

namespace DisprzTraining.Business
{
    public interface IUserService
    {

        // Authenticate user
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User> RegisterAsync(string username,string firstName, string lastName, string password, string? timeZoneId = null);
        Task<(bool Success, string? Error)> UpdateTimeZoneAsync(int userId, string timeZoneId);  
        string GenerateJwtToken(User user);
    }
}
