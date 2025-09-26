using DisprzTraining.DataAccess;
using DisprzTraining.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DisprzTraining.Tests
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new UserRepository(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "john.doe",
                    FirstName = "John",
                    LastName = "Doe",
                    TimeZoneId = "UTC",
                    PasswordHash = "hashedpassword1"
                },
                new User
                {
                    Id = 2,
                    Username = "jane.smith",
                    FirstName = "Jane",
                    LastName = "Smith",
                    TimeZoneId = "America/New_York",
                    PasswordHash = "hashedpassword2"
                },
                new User
                {
                    Id = 3,
                    Username = "test.user",
                    FirstName = "Test",
                    LastName = "User",
                    TimeZoneId = "Europe/London",
                    PasswordHash = "hashedpassword3"
                },
                new User
                {
                    Id = 4,
                    Username = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    TimeZoneId = "Asia/Tokyo",
                    PasswordHash = "hashedpassword4"
                }
            };

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "John's Meeting",
                    Description = "Important meeting",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                    UserId = 1,
                    Type = "Meeting",
                    ColorCode = "#FF0000",
                    Recurrence = RecurrenceType.None
                },
                new Appointment
                {
                    Id = 2,
                    Title = "John's Call",
                    Description = "Client call",
                    StartTime = DateTime.UtcNow.AddDays(2),
                    EndTime = DateTime.UtcNow.AddDays(2).AddHours(1),
                    UserId = 1,
                    Type = "Call",
                    ColorCode = "#00FF00",
                    Recurrence = RecurrenceType.Weekly
                },
                new Appointment
                {
                    Id = 3,
                    Title = "Jane's Review",
                    Description = "Performance review",
                    StartTime = DateTime.UtcNow.AddDays(3),
                    EndTime = DateTime.UtcNow.AddDays(3).AddHours(2),
                    UserId = 2,
                    Type = "Review",
                    ColorCode = "#0000FF",
                    Recurrence = RecurrenceType.None
                }
            };

            _context.Users.AddRange(users);
            _context.Appointments.AddRange(appointments);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ValidId_ReturnsUserWithAppointments()
        {
            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("john.doe", result.Username);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
            Assert.Equal("UTC", result.TimeZoneId);
            Assert.Equal("hashedpassword1", result.PasswordHash);
            
            // Verify appointments are included
            Assert.NotNull(result.Appointments);
            Assert.Equal(2, result.Appointments.Count);
            Assert.Contains(result.Appointments, a => a.Title == "John's Meeting");
            Assert.Contains(result.Appointments, a => a.Title == "John's Call");
        }

        [Fact]
        public async Task GetByIdAsync_UserWithNoAppointments_ReturnsUserWithEmptyAppointments()
        {
            // Act
            var result = await _repository.GetByIdAsync(3); // Test user with no appointments

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Id);
            Assert.Equal("test.user", result.Username);
            Assert.NotNull(result.Appointments);
            Assert.Empty(result.Appointments);
        }

        [Fact]
        public async Task GetByIdAsync_InvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ZeroId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_NegativeId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByUsernameAsync Tests

        [Fact]
        public async Task GetByUsernameAsync_ValidUsername_ReturnsUserWithAppointments()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("jane.smith");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
            Assert.Equal("jane.smith", result.Username);
            Assert.Equal("Jane", result.FirstName);
            Assert.Equal("Smith", result.LastName);
            Assert.Equal("America/New_York", result.TimeZoneId);
            
            // Verify appointments are included
            Assert.NotNull(result.Appointments);
            Assert.Single(result.Appointments);
            Assert.Equal("Jane's Review", result.Appointments.First().Title);
        }

        [Fact]
        public async Task GetByUsernameAsync_EmailAsUsername_ReturnsUser()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("admin@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Id);
            Assert.Equal("admin@example.com", result.Username);
            Assert.Equal("Admin", result.FirstName);
            Assert.Equal("User", result.LastName);
        }

        [Fact]
        public async Task GetByUsernameAsync_InvalidUsername_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("nonexistent.user");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUsernameAsync_EmptyUsername_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUsernameAsync_NullUsername_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByUsernameAsync(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUsernameAsync_CaseSensitive_ReturnsNull()
        {
            // Act - Username is case sensitive
            var result = await _repository.GetByUsernameAsync("JOHN.DOE");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUsernameAsync_WhitespaceUsername_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("   ");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_ValidUser_AddsToDatabase()
        {
            // Arrange
            var newUser = new User
            {
                Username = "new.user",
                FirstName = "New",
                LastName = "User",
                TimeZoneId = "Australia/Sydney",
                PasswordHash = "newhashedpassword"
            };

            // Act
            await _repository.AddAsync(newUser);

            // Assert
            var addedUser = await _context.Users.FindAsync(newUser.Id);
            Assert.NotNull(addedUser);
            Assert.Equal("new.user", addedUser.Username);
            Assert.Equal("New", addedUser.FirstName);
            Assert.Equal("User", addedUser.LastName);
            Assert.Equal("Australia/Sydney", addedUser.TimeZoneId);
            Assert.Equal("newhashedpassword", addedUser.PasswordHash);
        }

        [Fact]
        public async Task AddAsync_UserWithSpecialCharacters_AddsSuccessfully()
        {
            // Arrange
            var newUser = new User
            {
                Username = "user@domain.com",
                FirstName = "José",
                LastName = "O'Connor-Smith",
                TimeZoneId = "America/Sao_Paulo",
                PasswordHash = "specialhashedpassword"
            };

            // Act
            await _repository.AddAsync(newUser);

            // Assert
            var addedUser = await _context.Users.FindAsync(newUser.Id);
            Assert.NotNull(addedUser);
            Assert.Equal("user@domain.com", addedUser.Username);
            Assert.Equal("José", addedUser.FirstName);
            Assert.Equal("O'Connor-Smith", addedUser.LastName);
        }

        [Fact]
        public async Task AddAsync_UserWithEmptyOptionalFields_AddsSuccessfully()
        {
            // Arrange
            var newUser = new User
            {
                Username = "minimal.user",
                FirstName = "",
                LastName = "",
                TimeZoneId = "UTC",
                PasswordHash = "minimalhashedpassword"
            };

            // Act
            await _repository.AddAsync(newUser);

            // Assert
            var addedUser = await _context.Users.FindAsync(newUser.Id);
            Assert.NotNull(addedUser);
            Assert.Equal("minimal.user", addedUser.Username);
            Assert.Equal("", addedUser.FirstName);
            Assert.Equal("", addedUser.LastName);
        }

        [Fact]
        public async Task AddAsync_MultipleUsers_AddsAllSuccessfully()
        {
            // Arrange
            var initialCount = await _context.Users.CountAsync();
            var users = new List<User>
            {
                new User
                {
                    Username = "batch.user1",
                    FirstName = "Batch1",
                    LastName = "User1",
                    TimeZoneId = "UTC",
                    PasswordHash = "hash1"
                },
                new User
                {
                    Username = "batch.user2",
                    FirstName = "Batch2",
                    LastName = "User2",
                    TimeZoneId = "UTC",
                    PasswordHash = "hash2"
                }
            };

            // Act
            foreach (var user in users)
            {
                await _repository.AddAsync(user);
            }

            // Assert
            var finalCount = await _context.Users.CountAsync();
            Assert.Equal(initialCount + 2, finalCount);

            var addedUser1 = await _repository.GetByUsernameAsync("batch.user1");
            var addedUser2 = await _repository.GetByUsernameAsync("batch.user2");
            
            Assert.NotNull(addedUser1);
            Assert.NotNull(addedUser2);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ValidUser_UpdatesInDatabase()
        {
            // Arrange
            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user);

            user.FirstName = "Updated John";
            user.LastName = "Updated Doe";
            user.TimeZoneId = "America/Los_Angeles";

            // Act
            await _repository.UpdateAsync(user);

            // Assert
            var updatedUser = await _context.Users.FindAsync(1);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated John", updatedUser.FirstName);
            Assert.Equal("Updated Doe", updatedUser.LastName);
            Assert.Equal("America/Los_Angeles", updatedUser.TimeZoneId);
            Assert.Equal("john.doe", updatedUser.Username); // Username should remain unchanged
        }

        [Fact]
        public async Task UpdateAsync_UpdateUsername_UpdatesSuccessfully()
        {
            // Arrange
            var user = await _context.Users.FindAsync(2);
            Assert.NotNull(user);

            user.Username = "updated.jane.smith";

            // Act
            await _repository.UpdateAsync(user);

            // Assert
            var updatedUser = await _context.Users.FindAsync(2);
            Assert.NotNull(updatedUser);
            Assert.Equal("updated.jane.smith", updatedUser.Username);
            
            // Verify old username no longer exists
            var oldUser = await _repository.GetByUsernameAsync("jane.smith");
            Assert.Null(oldUser);
            
            // Verify new username works
            var newUser = await _repository.GetByUsernameAsync("updated.jane.smith");
            Assert.NotNull(newUser);
            Assert.Equal(2, newUser.Id);
        }

        [Fact]
        public async Task UpdateAsync_UpdatePasswordHash_UpdatesSuccessfully()
        {
            // Arrange
            var user = await _context.Users.FindAsync(3);
            Assert.NotNull(user);

            var originalPasswordHash = user.PasswordHash;
            user.PasswordHash = "newhashedpassword123";

            // Act
            await _repository.UpdateAsync(user);

            // Assert
            var updatedUser = await _context.Users.FindAsync(3);
            Assert.NotNull(updatedUser);
            Assert.Equal("newhashedpassword123", updatedUser.PasswordHash);
            Assert.NotEqual(originalPasswordHash, updatedUser.PasswordHash);
        }

        [Fact]
        public async Task UpdateAsync_UpdateAllFields_UpdatesSuccessfully()
        {
            // Arrange
            var user = await _context.Users.FindAsync(4);
            Assert.NotNull(user);

            user.Username = "completely.new.user";
            user.FirstName = "Completely";
            user.LastName = "New";
            user.TimeZoneId = "Pacific/Auckland";
            user.PasswordHash = "completelynewpassword";

            // Act
            await _repository.UpdateAsync(user);

            // Assert
            var updatedUser = await _context.Users.FindAsync(4);
            Assert.NotNull(updatedUser);
            Assert.Equal("completely.new.user", updatedUser.Username);
            Assert.Equal("Completely", updatedUser.FirstName);
            Assert.Equal("New", updatedUser.LastName);
            Assert.Equal("Pacific/Auckland", updatedUser.TimeZoneId);
            Assert.Equal("completelynewpassword", updatedUser.PasswordHash);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Repository_AddUpdateRetrieve_Workflow()
        {
            // Arrange & Act - Add
            var newUser = new User
            {
                Username = "workflow.test",
                FirstName = "Workflow",
                LastName = "Test",
                TimeZoneId = "Europe/Berlin",
                PasswordHash = "workflowpassword"
            };

            await _repository.AddAsync(newUser);
            var addedId = newUser.Id;

            // Assert - Add
            var addedUser = await _repository.GetByIdAsync(addedId);
            Assert.NotNull(addedUser);
            Assert.Equal("workflow.test", addedUser.Username);

            // Act - Update
            addedUser.FirstName = "Updated Workflow";
            addedUser.TimeZoneId = "America/Chicago";
            await _repository.UpdateAsync(addedUser);

            // Assert - Update
            var updatedUser = await _repository.GetByIdAsync(addedId);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated Workflow", updatedUser.FirstName);
            Assert.Equal("America/Chicago", updatedUser.TimeZoneId);

            // Act & Assert - Retrieve by Username
            var retrievedUser = await _repository.GetByUsernameAsync("workflow.test");
            Assert.NotNull(retrievedUser);
            Assert.Equal(addedId, retrievedUser.Id);
            Assert.Equal("Updated Workflow", retrievedUser.FirstName);
        }

        [Fact]
        public async Task Repository_UserWithAppointments_MaintainsRelationship()
        {
            // Arrange - Add user
            var newUser = new User
            {
                Username = "user.with.appointments",
                FirstName = "User",
                LastName = "WithAppointments",
                TimeZoneId = "UTC",
                PasswordHash = "password"
            };

            await _repository.AddAsync(newUser);

            // Add appointment for the user
            var appointment = new Appointment
            {
                Title = "Test Appointment",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                UserId = newUser.Id,
                Type = "Test",
                ColorCode = "#123456",
                Recurrence = RecurrenceType.None
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Act - Retrieve user
            var userWithAppointments = await _repository.GetByIdAsync(newUser.Id);

            // Assert
            Assert.NotNull(userWithAppointments);
            Assert.NotNull(userWithAppointments.Appointments);
            Assert.Single(userWithAppointments.Appointments);
            Assert.Equal("Test Appointment", userWithAppointments.Appointments.First().Title);
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Fact]
        public async Task GetByUsernameAsync_VeryLongUsername_HandlesCorrectly()
        {
            // Arrange
            var longUsername = new string('a', 100) + "@example.com";
            var user = new User
            {
                Username = longUsername,
                FirstName = "Long",
                LastName = "Username",
                TimeZoneId = "UTC",
                PasswordHash = "password"
            };

            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByUsernameAsync(longUsername);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(longUsername, result.Username);
        }

        [Fact]
        public async Task AddAsync_UserWithUnicodeCharacters_AddsSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Username = "用户@example.com",
                FirstName = "测试",
                LastName = "用户",
                TimeZoneId = "Asia/Shanghai",
                PasswordHash = "密码哈希"
            };

            // Act
            await _repository.AddAsync(user);

            // Assert
            var addedUser = await _repository.GetByUsernameAsync("用户@example.com");
            Assert.NotNull(addedUser);
            Assert.Equal("测试", addedUser.FirstName);
            Assert.Equal("用户", addedUser.LastName);
        }

        [Fact]
        public async Task Repository_EmptyDatabase_HandlesCorrectly()
        {
            // Arrange - Create new context with no data
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var emptyContext = new AppDbContext(options);
            var emptyRepository = new UserRepository(emptyContext);

            // Act & Assert
            var userById = await emptyRepository.GetByIdAsync(1);
            Assert.Null(userById);

            var userByUsername = await emptyRepository.GetByUsernameAsync("nonexistent");
            Assert.Null(userByUsername);
        }

        #endregion

        #region Concurrent Operations Tests

        [Fact]
        public async Task Repository_ConcurrentReads_HandleCorrectly()
        {
            // Act - Multiple concurrent reads
            var tasks = new List<Task<User?>>
            {
                _repository.GetByIdAsync(1),
                _repository.GetByIdAsync(2),
                _repository.GetByUsernameAsync("john.doe"),
                _repository.GetByUsernameAsync("jane.smith")
            };

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.NotNull(result));
            Assert.Equal("john.doe", results[0]?.Username);
            Assert.Equal("jane.smith", results[1]?.Username);
            Assert.Equal("john.doe", results[2]?.Username);
            Assert.Equal("jane.smith", results[3]?.Username);
        }

        #endregion
    }
}
