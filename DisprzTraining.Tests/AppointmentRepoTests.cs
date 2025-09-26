using DisprzTraining.DataAccess;
using DisprzTraining.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DisprzTraining.Tests
{
    public class AppointmentRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AppointmentRepository _repository;

        public AppointmentRepositoryTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new AppointmentRepository(_context);

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
                    Username = "user1",
                    FirstName = "John",
                    LastName = "Doe",
                    TimeZoneId = "UTC",
                    PasswordHash = "hashedpassword1"
                },
                new User
                {
                    Id = 2,
                    Username = "user2",
                    FirstName = "Jane",
                    LastName = "Smith",
                    TimeZoneId = "America/New_York",
                    PasswordHash = "hashedpassword2"
                },
                new User
                {
                    Id = 3,
                    Username = "testuser",
                    FirstName = "Test",
                    LastName = "User",
                    TimeZoneId = "Europe/London",
                    PasswordHash = "hashedpassword3"
                }
            };

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Team Meeting",
                    Description = "Weekly team sync",
                    Location = "Conference Room A",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                    UserId = 1,
                    Attendees = "john@example.com,jane@example.com",
                    Type = "Meeting",
                    ColorCode = "#FF0000",
                    Recurrence = RecurrenceType.Weekly
                },
                new Appointment
                {
                    Id = 2,
                    Title = "Project Review",
                    Description = "Quarterly project review",
                    Location = "Conference Room B",
                    StartTime = DateTime.UtcNow.AddDays(2),
                    EndTime = DateTime.UtcNow.AddDays(2).AddHours(2),
                    UserId = 1,
                    Attendees = "manager@example.com",
                    Type = "Review",
                    ColorCode = "#00FF00",
                    Recurrence = RecurrenceType.None
                },
                new Appointment
                {
                    Id = 3,
                    Title = "Client Call",
                    Description = "Important client discussion",
                    Location = "Phone",
                    StartTime = DateTime.UtcNow.AddDays(3),
                    EndTime = DateTime.UtcNow.AddDays(3).AddHours(1),
                    UserId = 2,
                    Attendees = "client@example.com",
                    Type = "Call",
                    ColorCode = "#0000FF",
                    Recurrence = RecurrenceType.None
                },
                new Appointment
                {
                    Id = 4,
                    Title = "Training Session",
                    Description = "Technical training",
                    Location = "Training Room",
                    StartTime = DateTime.UtcNow.AddDays(4),
                    EndTime = DateTime.UtcNow.AddDays(4).AddHours(3),
                    UserId = 2,
                    Attendees = "team@example.com",
                    Type = "Training",
                    ColorCode = "#FFFF00",
                    Recurrence = RecurrenceType.Monthly
                },
                new Appointment
                {
                    Id = 5,
                    Title = "Doctor Appointment",
                    Description = null,
                    Location = null,
                    StartTime = DateTime.UtcNow.AddDays(5),
                    EndTime = DateTime.UtcNow.AddDays(5).AddHours(1),
                    UserId = 3,
                    Attendees = null,
                    Type = "Personal",
                    ColorCode = "#FF00FF",
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

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllAppointments()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(5, result.Count);
            Assert.All(result, a => Assert.NotNull(a.User)); // Verify User is included
        }

        [Fact]
        public async Task GetAllAsync_IncludesUserData()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            var appointment = result.First(a => a.Id == 1);
            Assert.NotNull(appointment.User);
            Assert.Equal("user1", appointment.User.Username);
            Assert.Equal("John", appointment.User.FirstName);
        }

        [Fact]
        public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Arrange - Create new context with no data
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var emptyContext = new AppDbContext(options);
            var emptyRepository = new AppointmentRepository(emptyContext);

            // Act
            var result = await emptyRepository.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ValidId_ReturnsAppointmentWithUser()
        {
            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Team Meeting", result.Title);
            Assert.NotNull(result.User);
            Assert.Equal("user1", result.User.Username);
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

        #region GetByUserIdAsync Tests

        [Fact]
        public async Task GetByUserIdAsync_ValidUserId_ReturnsUserAppointmentsOrderedByStartTime()
        {
            // Act
            var result = await _repository.GetByUserIdAsync(1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Team Meeting", result[0].Title);
            Assert.Equal("Project Review", result[1].Title);
            
            // Verify ordering by StartTime
            Assert.True(result[0].StartTime <= result[1].StartTime);
        }

        [Fact]
        public async Task GetByUserIdAsync_UserWithNoAppointments_ReturnsEmptyList()
        {
            // Arrange - Add user with no appointments
            var newUser = new User
            {
                Id = 99,
                Username = "noappointments",
                FirstName = "No",
                LastName = "Appointments",
                TimeZoneId = "UTC",
                PasswordHash = "hash"
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUserIdAsync(99);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_InvalidUserId_ReturnsEmptyList()
        {
            // Act
            var result = await _repository.GetByUserIdAsync(999);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_MultipleUsers_ReturnsOnlySpecificUserAppointments()
        {
            // Act
            var user1Appointments = await _repository.GetByUserIdAsync(1);
            var user2Appointments = await _repository.GetByUserIdAsync(2);

            // Assert
            Assert.Equal(2, user1Appointments.Count);
            Assert.Equal(2, user2Appointments.Count);
            Assert.All(user1Appointments, a => Assert.Equal(1, a.UserId));
            Assert.All(user2Appointments, a => Assert.Equal(2, a.UserId));
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_ValidAppointment_AddsToDatabase()
        {
            // Arrange
            var newAppointment = new Appointment
            {
                Title = "New Meeting",
                Description = "Test meeting",
                StartTime = DateTime.UtcNow.AddDays(10),
                EndTime = DateTime.UtcNow.AddDays(10).AddHours(1),
                UserId = 1,
                Type = "Meeting",
                ColorCode = "#123456",
                Recurrence = RecurrenceType.None
            };

            // Act
            await _repository.AddAsync(newAppointment);

            // Assert
            var addedAppointment = await _context.Appointments.FindAsync(newAppointment.Id);
            Assert.NotNull(addedAppointment);
            Assert.Equal("New Meeting", addedAppointment.Title);
            Assert.Equal("Test meeting", addedAppointment.Description);
        }

        [Fact]
        public async Task AddAsync_AppointmentWithNullOptionalFields_AddsSuccessfully()
        {
            // Arrange
            var newAppointment = new Appointment
            {
                Title = "Simple Meeting",
                Description = null,
                Location = null,
                Attendees = null,
                StartTime = DateTime.UtcNow.AddDays(10),
                EndTime = DateTime.UtcNow.AddDays(10).AddHours(1),
                UserId = 1,
                Type = "Meeting",
                ColorCode = "#123456",
                Recurrence = RecurrenceType.None
            };

            // Act
            await _repository.AddAsync(newAppointment);

            // Assert
            var addedAppointment = await _context.Appointments.FindAsync(newAppointment.Id);
            Assert.NotNull(addedAppointment);
            Assert.Equal("Simple Meeting", addedAppointment.Title);
            Assert.Null(addedAppointment.Description);
            Assert.Null(addedAppointment.Location);
            Assert.Null(addedAppointment.Attendees);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ValidAppointment_UpdatesInDatabase()
        {
            // Arrange
            var appointment = await _context.Appointments.FindAsync(1);
            Assert.NotNull(appointment);
            
            appointment.Title = "Updated Team Meeting";
            appointment.Description = "Updated description";
            appointment.Location = "Updated Location";

            // Act
            await _repository.UpdateAsync(appointment);

            // Assert
            var updatedAppointment = await _context.Appointments.FindAsync(1);
            Assert.NotNull(updatedAppointment);
            Assert.Equal("Updated Team Meeting", updatedAppointment.Title);
            Assert.Equal("Updated description", updatedAppointment.Description);
            Assert.Equal("Updated Location", updatedAppointment.Location);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ValidAppointment_RemovesFromDatabase()
        {
            // Arrange
            var appointment = await _context.Appointments.FindAsync(1);
            Assert.NotNull(appointment);
            var initialCount = await _context.Appointments.CountAsync();

            // Act
            await _repository.DeleteAsync(appointment);

            // Assert
            var deletedAppointment = await _context.Appointments.FindAsync(1);
            Assert.Null(deletedAppointment);
            
            var finalCount = await _context.Appointments.CountAsync();
            Assert.Equal(initialCount - 1, finalCount);
        }

        #endregion

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_SearchByTitle_ReturnsMatchingAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("meeting", null);

            // Assert
            Assert.Single(result); // Only "Team Meeting"
            Assert.Contains(result, a => a.Title.ToLower().Contains("meeting"));
        }

        [Fact]
        public async Task SearchAsync_SearchByDescription_ReturnsMatchingAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("weekly", null);

            // Assert
            Assert.Single(result);
            Assert.Equal("Team Meeting", result[0].Title);
        }

        [Fact]
        public async Task SearchAsync_SearchByLocation_ReturnsMatchingAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("conference", null);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, a => Assert.Contains("Conference", a.Location ?? ""));
        }

        [Fact]
        public async Task SearchAsync_SearchByAttendees_ReturnsMatchingAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("jane@example.com", null);

            // Assert
            Assert.Single(result);
            Assert.Equal("Team Meeting", result[0].Title);
        }

        [Fact]
        public async Task SearchAsync_SearchByUsername_ReturnsMatchingAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("user1", null);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, a => Assert.Equal("user1", a.User.Username));
        }

                [Fact]
        public async Task SearchAsync_CaseInsensitiveSearch_ReturnsMatchingAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("TEAM", null);

            // Assert
            Assert.Equal(2, result.Count); // "Team Meeting" (title) and "Training Session" (attendees: team@example.com)
            Assert.Contains(result, a => a.Title.ToLower().Contains("team") || 
                                        (a.Attendees != null && a.Attendees.ToLower().Contains("team")));
        }


        [Fact]
        public async Task SearchAsync_WithUserId_ReturnsOnlyUserAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("meeting", 1);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].UserId);
            Assert.Equal("Team Meeting", result[0].Title);
        }

        [Fact]
        public async Task SearchAsync_NoMatches_ReturnsEmptyList()
        {
            // Act
            var result = await _repository.SearchAsync("nonexistentkeyword", null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_EmptyKeyword_ReturnsAllAppointments()
        {
            // Act
            var result = await _repository.SearchAsync("", null);

            // Assert
            Assert.Equal(5, result.Count); // All seeded appointments
        }

        #endregion

        #region GetUserByIdAsync Tests

        [Fact]
        public async Task GetUserByIdAsync_ValidUserId_ReturnsUser()
        {
            // Act
            var result = await _repository.GetUserByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("user1", result.Username);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
        }

        [Fact]
        public async Task GetUserByIdAsync_InvalidUserId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetUserByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_ZeroUserId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetUserByIdAsync(0);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Repository_AddUpdateDelete_Workflow()
        {
            // Arrange & Act - Add
            var newAppointment = new Appointment
            {
                Title = "Workflow Test",
                StartTime = DateTime.UtcNow.AddDays(15),
                EndTime = DateTime.UtcNow.AddDays(15).AddHours(1),
                UserId = 1,
                Type = "Test",
                ColorCode = "#ABCDEF",
                Recurrence = RecurrenceType.None
            };

            await _repository.AddAsync(newAppointment);
            var addedId = newAppointment.Id;

            // Assert - Add
            var addedAppointment = await _repository.GetByIdAsync(addedId);
            Assert.NotNull(addedAppointment);
            Assert.Equal("Workflow Test", addedAppointment.Title);

            // Act - Update
            addedAppointment.Title = "Updated Workflow Test";
            addedAppointment.Description = "Updated description";
            await _repository.UpdateAsync(addedAppointment);

            // Assert - Update
            var updatedAppointment = await _repository.GetByIdAsync(addedId);
            Assert.NotNull(updatedAppointment);
            Assert.Equal("Updated Workflow Test", updatedAppointment.Title);
            Assert.Equal("Updated description", updatedAppointment.Description);

            // Act - Delete
            await _repository.DeleteAsync(updatedAppointment);

            // Assert - Delete
            var deletedAppointment = await _repository.GetByIdAsync(addedId);
            Assert.Null(deletedAppointment);
        }

        #endregion
    }
}
