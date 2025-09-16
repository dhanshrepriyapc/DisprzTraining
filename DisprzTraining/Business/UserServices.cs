using DisprzTraining.DataAccess;
using DisprzTraining.Models;

namespace DisprzTraining.Business
{
    public class UserService
    {
        private readonly UserRepository _repository;

        public UserService(UserRepository repository)
        {
            _repository = repository;
        }

        // Authenticate user
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _repository.GetByUsernameAsync(username);
            if (user == null) return null;

            // For now plain text check (later replace with hashing)
            return user.PasswordHash == password ? user : null;
        }

        // Register new user
        public async Task<User> RegisterAsync(string username, string password)
        {
            var existing = await _repository.GetByUsernameAsync(username);
            if (existing != null) throw new Exception("Username already exists");

            var user = new User
            {
                Username = username,
                PasswordHash = password
            };

            await _repository.AddAsync(user);
            return user;
        }
    }
}
