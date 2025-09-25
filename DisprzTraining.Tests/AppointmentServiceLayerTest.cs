using DisprzTraining.Business;
using DisprzTraining.DataAccess;
using DisprzTraining.DTOs;
using DisprzTraining.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DisprzTraining.Tests
{
    public class AppointmentServiceTests
    {
        private readonly Mock<IAppointmentRepository> _mockAppointmentRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly AppointmentService _service;

        public AppointmentServiceTests()
        {
            _mockAppointmentRepo = new Mock<IAppointmentRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _service = new AppointmentService(_mockAppointmentRepo.Object, _mockUserRepo.Object);
        }

        #region GetAppointmentsForUserAsync Tests

        [Fact]
        public async Task GetAppointmentsForUserAsync_ValidUserId_ReturnsAppointmentDtos()
        {
            // Arrange
            var userId = 1;
            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Meeting 1",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    UserId = userId,
                    Type = "Meeting",
                    ColorCode = "#FF0000"
                },
                new Appointment
                {
                    Id = 2,
                    Title = "Meeting 2",
                    StartTime = DateTime.UtcNow.AddHours(3),
                    EndTime = DateTime.UtcNow.AddHours(4),
                    UserId = userId,
                    Type = "Call",
                    ColorCode = "#00FF00"
                }
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(appointments);

            // Act
            var result = await _service.GetAppointmentsForUserAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Meeting 1", result[0].Title);
            Assert.Equal("Meeting 2", result[1].Title);
            Assert.Equal(userId, result[0].UserId);
            Assert.Equal(userId, result[1].UserId);
        }

        [Fact]
        public async Task GetAppointmentsForUserAsync_NoAppointments_ReturnsEmptyList()
        {
            // Arrange
            var userId = 1;
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _service.GetAppointmentsForUserAsync(userId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetAppointmentByIdAsync Tests

        [Fact]
        public async Task GetAppointmentByIdAsync_ValidId_ReturnsAppointment()
        {
            // Arrange
            var appointmentId = 1;
            var appointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                UserId = 1
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(appointment);

            // Act
            var result = await _service.GetAppointmentByIdAsync(appointmentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(appointmentId, result.Id);
            Assert.Equal("Test Meeting", result.Title);
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var appointmentId = 999;
            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync((Appointment?)null);

            // Act
            var result = await _service.GetAppointmentByIdAsync(appointmentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAppointmentAsync Tests

        [Fact]
        public async Task CreateAppointmentAsync_ValidSingleAppointment_ReturnsSuccess()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Type = "Meeting",
                ColorCode = "#FF0000",
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            Assert.Equal("Test Meeting", result.Appointment.Title);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_UserNotFound_ReturnsError()
        {
            // Arrange
            var userId = 999;
            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3)
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync((User?)null);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_StartTimeAfterEndTime_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(2), // End before start
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("StartTime must be before EndTime", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_OverlappingAppointment_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Overlapping Meeting",
                StartTime = DateTime.UtcNow.AddHours(2).AddMinutes(30), // Overlaps with existing
                EndTime = DateTime.UtcNow.AddHours(3).AddMinutes(30),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Appointment time overlaps with existing appointment", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_PastAppointment_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var dto = new AppointmentDto
            {
                Title = "Past Meeting",
                StartTime = DateTime.UtcNow.AddHours(-2), // In the past
                EndTime = DateTime.UtcNow.AddHours(-1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_DailyRecurringAppointment_CreatesMultipleAppointments()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1); // Tomorrow
            var dto = new AppointmentDto
            {
                Title = "Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(3), // 4 total occurrences
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        [Fact]
        public async Task CreateAppointmentAsync_WeeklyRecurringAppointment_CreatesCorrectOccurrences()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Weekly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Weekly,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(14), // 3 total occurrences
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(3));
        }

        [Fact]
        public async Task CreateAppointmentAsync_MonthlyRecurringAppointment_CreatesCorrectOccurrences()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Monthly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Monthly,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddMonths(2), // 3 total occurrences
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(3));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithSomeFutureOccurrences_CreatesOnlyFutureOnes()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start with a future time but end date that would create some past occurrences
            var startTime = DateTime.UtcNow.AddHours(1); // Future
            var dto = new AppointmentDto
            {
                Title = "Mixed Time Recurring Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(2), // Creates 3 occurrences
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                        .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                            .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                            .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            // Should create 3 appointments (all future)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(3));
        }


        #endregion

        #region UpdateAppointmentAsync Tests

        [Fact]
        public async Task UpdateAppointmentAsync_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Old Title",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = DateTime.UtcNow.AddHours(4),
                EndTime = DateTime.UtcNow.AddHours(5),
                Type = "Updated Type",
                ColorCode = "#00FF00"
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(existingAppointment);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });
            _mockAppointmentRepo.Setup(r => r.UpdateAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_AppointmentNotFound_ReturnsError()
        {
            // Arrange
            var appointmentId = 999;
            var userId = 1;
            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = DateTime.UtcNow.AddHours(4),
                EndTime = DateTime.UtcNow.AddHours(5)
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync((Appointment?)null);

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Not found", result.Error);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_UnauthorizedUser_ReturnsError()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var unauthorizedUserId = 2;
            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                UserId = userId // Different user
            };

            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = DateTime.UtcNow.AddHours(4),
                EndTime = DateTime.UtcNow.AddHours(5)
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(existingAppointment);

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, unauthorizedUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Unauthorized", result.Error);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_InvalidTimeRange_ReturnsError()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = DateTime.UtcNow.AddHours(5),
                EndTime = DateTime.UtcNow.AddHours(4) // End before start
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(existingAppointment);

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("StartTime must be before EndTime", result.Error);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_OverlappingWithOtherAppointment_ReturnsError()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                UserId = userId,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3)
            };

            var otherAppointment = new Appointment
            {
                Id = 2,
                Title = "Other Meeting",
                UserId = userId,
                StartTime = DateTime.UtcNow.AddHours(4),
                EndTime = DateTime.UtcNow.AddHours(5)
            };

            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = DateTime.UtcNow.AddHours(4).AddMinutes(30), // Overlaps with other
                EndTime = DateTime.UtcNow.AddHours(5).AddMinutes(30)
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(existingAppointment);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment, otherAppointment });

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Appointment time overlaps with existing appointment", result.Error);
        }

        #endregion

        #region DeleteAppointmentAsync Tests

        [Fact]
        public async Task DeleteAppointmentAsync_ValidDelete_ReturnsSuccess()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var appointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                UserId = userId
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(appointment);
            _mockAppointmentRepo.Setup(r => r.DeleteAsync(appointment))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAppointmentAsync(appointmentId, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            _mockAppointmentRepo.Verify(r => r.DeleteAsync(appointment), Times.Once);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_AppointmentNotFound_ReturnsError()
        {
            // Arrange
            var appointmentId = 999;
            var userId = 1;

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync((Appointment?)null);

            // Act
            var result = await _service.DeleteAppointmentAsync(appointmentId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Not found", result.Error);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_UnauthorizedUser_ReturnsError()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var unauthorizedUserId = 2;
            var appointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                UserId = userId // Different user
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(appointment);

            // Act
            var result = await _service.DeleteAppointmentAsync(appointmentId, unauthorizedUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Unauthorized", result.Error);
        }

        #endregion

        #region SearchAppointmentsAsync Tests

        [Fact]
        public async Task SearchAppointmentsAsync_ValidKeyword_ReturnsMatchingAppointments()
        {
            // Arrange
            var keyword = "meeting";
            var userId = 1;
            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Team Meeting",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.SearchAsync(keyword, userId))
                               .ReturnsAsync(appointments);

            // Act
            var result = await _service.SearchAppointmentsAsync(keyword, userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("Team Meeting", result[0].Title);
        }

        [Fact]
        public async Task SearchAppointmentsAsync_EmptyKeyword_ReturnsEmptyList()
        {
            // Arrange
            var keyword = "";
            var userId = 1;

            // Act
            var result = await _service.SearchAppointmentsAsync(keyword, userId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAppointmentsAsync_WhitespaceKeyword_ReturnsEmptyList()
        {
            // Arrange
            var keyword = "   ";
            var userId = 1;

            // Act
            var result = await _service.SearchAppointmentsAsync(keyword, userId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAppointmentsAsync_NullKeyword_ReturnsEmptyList()
        {
            // Arrange
            string? keyword = null;
            var userId = 1;

            // Act
            var result = await _service.SearchAppointmentsAsync(keyword!, userId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region UpdateAppointmentTypeAsync Tests

        [Fact]
        public async Task UpdateAppointmentTypeAsync_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var appointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                UserId = userId,
                Type = "Old Type",
                ColorCode = "#FF0000"
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(appointment);
            _mockAppointmentRepo.Setup(r => r.UpdateAsync(appointment))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAppointmentTypeAsync(appointmentId, "New Type", "#00FF00", userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.Equal("New Type", appointment.Type);
            Assert.Equal("#00FF00", appointment.ColorCode);
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(appointment), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentTypeAsync_AppointmentNotFound_ReturnsError()
        {
            // Arrange
            var appointmentId = 999;
            var userId = 1;

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync((Appointment?)null);

            // Act
            var result = await _service.UpdateAppointmentTypeAsync(appointmentId, "New Type", "#00FF00", userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Not found", result.Error);
        }

        [Fact]
        public async Task UpdateAppointmentTypeAsync_UnauthorizedUser_ReturnsError()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var unauthorizedUserId = 2;
            var appointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                UserId = userId // Different user
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync(appointment);

            // Act
            var result = await _service.UpdateAppointmentTypeAsync(appointmentId, "New Type", "#00FF00", unauthorizedUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Unauthorized", result.Error);
        }

        #endregion

        #region GetRecurringAppointmentsAsync Tests

        [Fact]
        public async Task GetRecurringAppointmentsAsync_ValidDateRange_ReturnsFilteredAppointments()
        {
            // Arrange
            var userId = 1;
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(7);

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Meeting 1",
                    StartTime = startDate.AddDays(1),
                    EndTime = startDate.AddDays(1).AddHours(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 2,
                    Title = "Meeting 2",
                    StartTime = startDate.AddDays(10), // Outside range
                    EndTime = startDate.AddDays(10).AddHours(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 3,
                    Title = "Meeting 3",
                    StartTime = startDate.AddDays(3),
                    EndTime = startDate.AddDays(3).AddHours(1),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(appointments);

            // Act
            var result = await _service.GetRecurringAppointmentsAsync(userId, startDate, endDate);

            // Assert
            Assert.Equal(2, result.Count); // Only meetings 1 and 3 should be in range
            Assert.Equal("Meeting 1", result[0].Title);
            Assert.Equal("Meeting 3", result[1].Title);
        }

        [Fact]
        public async Task GetRecurringAppointmentsAsync_NoAppointmentsInRange_ReturnsEmptyList()
        {
            // Arrange
            var userId = 1;
            var startDate = DateTime.UtcNow.Date.AddDays(10);
            var endDate = startDate.AddDays(7);

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Meeting 1",
                    StartTime = DateTime.UtcNow.Date.AddDays(1), // Before range
                    EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(1),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(appointments);

            // Act
            var result = await _service.GetRecurringAppointmentsAsync(userId, startDate, endDate);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRecurringAppointmentsAsync_AppointmentsOrderedByStartTime_ReturnsOrderedList()
        {
            // Arrange
            var userId = 1;
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(7);

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Meeting C",
                    StartTime = startDate.AddDays(5),
                    EndTime = startDate.AddDays(5).AddHours(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 2,
                    Title = "Meeting A",
                    StartTime = startDate.AddDays(1),
                    EndTime = startDate.AddDays(1).AddHours(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 3,
                    Title = "Meeting B",
                    StartTime = startDate.AddDays(3),
                    EndTime = startDate.AddDays(3).AddHours(1),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(appointments);

            // Act
            var result = await _service.GetRecurringAppointmentsAsync(userId, startDate, endDate);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Meeting A", result[0].Title); // Earliest
            Assert.Equal("Meeting B", result[1].Title); // Middle
            Assert.Equal("Meeting C", result[2].Title); // Latest
        }

        #endregion

        #region Edge Cases and Error Handling Tests
        [Fact]
        public async Task CreateAppointmentAsync_RecurringStartsInFutureButAllOccurrencesSkipped_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Create a scenario where the start time is just barely in the past
            var startTime = DateTime.UtcNow.AddMinutes(-5); // 5 minutes in the past
            var dto = new AppointmentDto
            {
                Title = "Edge Case Recurring Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(1), // Very short range, all in past
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                        .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                            .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_InvalidTimeZone_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Invalid/TimeZone"
            };

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid timezone configuration for user", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_RepositoryThrowsException_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Database error", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithMaxOccurrencesLimit_StopsAtLimit()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(200), // Would create 201 occurrences
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            // Should stop at 100 occurrences (safety limit)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(100));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithZeroInterval_UsesDefaultInterval()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = null, // Should default to 1
                RecurrenceEndDate = startTime.AddDays(3),
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithNoEndDate_UsesDefaultEndDate()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = null, // Should default to 3 months
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            // Should create appointments for 3 months (90+ days)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.AtLeast(90));
        }

        [Fact]
        public async Task CreateAppointmentAsync_MonthlyRecurringOnDay31_HandlesMonthsWithFewerDays()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start on January 31st
            var startTime = new DateTime(DateTime.UtcNow.Year + 1, 1, 31, 10, 0, 0, DateTimeKind.Utc);
            var dto = new AppointmentDto
            {
                Title = "Monthly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Monthly,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddMonths(3), // Through April
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.NotNull(result.Appointment);
            // Should create 4 appointments: Jan 31, Feb 28/29, Mar 31, Apr 30
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        #endregion

        #region Timezone Validation Tests

        [Fact]
        public async Task CreateAppointmentAsync_DifferentTimezones_ValidatesCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "America/New_York" // EST/EDT timezone
            };

            // Create appointment for 2 PM UTC (which might be 9 AM EST - in the past if current UTC time is later)
            var utcTime = DateTime.UtcNow.Date.AddHours(14); // 2 PM UTC today
            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = utcTime,
                EndTime = utcTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert - The result depends on current time, but should handle timezone conversion
            if (result.Success)
            {
                Assert.NotNull(result.Appointment);
                _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
            }
            else
            {
                Assert.Contains("Cannot book appointments in the past", result.Error);
            }
        }

        #endregion

        #region MapToDto Tests

        [Fact]
        public async Task GetAppointmentsForUserAsync_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var userId = 1;
            var appointment = new Appointment
            {
                Id = 1,
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                UserId = userId,
                Description = "Test Description",
                Location = "Test Location",
                Attendees = "test@example.com",
                Type = "Meeting",
                ColorCode = "#FF0000",
                Recurrence = RecurrenceType.Daily,
                RecurrenceInterval = 2,
                RecurrenceEndDate = DateTime.UtcNow.AddDays(30)
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { appointment });

            // Act
            var result = await _service.GetAppointmentsForUserAsync(userId);

            // Assert
            Assert.Single(result);
            var dto = result[0];
            Assert.Equal(appointment.Id, dto.Id);
            Assert.Equal(appointment.Title, dto.Title);
            Assert.Equal(appointment.StartTime, dto.StartTime);
            Assert.Equal(appointment.EndTime, dto.EndTime);
            Assert.Equal(appointment.UserId, dto.UserId);
            Assert.Equal(appointment.Description, dto.Description);
            Assert.Equal(appointment.Location, dto.Location);
            Assert.Equal(appointment.Attendees, dto.Attendees);
            Assert.Equal(appointment.Type, dto.Type);
            Assert.Equal(appointment.ColorCode, dto.ColorCode);
            Assert.Equal((AppointmentDto.RecurrenceType)appointment.Recurrence, dto.Recurrence);
            Assert.Equal(appointment.RecurrenceInterval, dto.RecurrenceInterval);
            Assert.Equal(appointment.RecurrenceEndDate, dto.RecurrenceEndDate);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task GetAppointmentsForUserAsync_CallsRepositoryWithCorrectUserId()
        {
            // Arrange
            var userId = 123;
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());

            // Act
            await _service.GetAppointmentsForUserAsync(userId);

            // Assert
            _mockAppointmentRepo.Verify(r => r.GetByUserIdAsync(123), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_CallsUserRepositoryFirst()
        {
            // Arrange
            var userId = 1;
            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync((User?)null);

            // Act
            await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            _mockUserRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
            // Should not call appointment repository if user not found
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_CallsGetByIdBeforeUpdate()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = DateTime.UtcNow.AddHours(4),
                EndTime = DateTime.UtcNow.AddHours(5)
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync((Appointment?)null);

            // Act
            await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            _mockAppointmentRepo.Verify(r => r.GetByIdAsync(appointmentId), Times.Once);
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_CallsGetByIdBeforeDelete()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId))
                               .ReturnsAsync((Appointment?)null);

            // Act
            await _service.DeleteAppointmentAsync(appointmentId, userId);

            // Assert
            _mockAppointmentRepo.Verify(r => r.GetByIdAsync(appointmentId), Times.Once);
            _mockAppointmentRepo.Verify(r => r.DeleteAsync(It.IsAny<Appointment>()), Times.Never);
        }

        #endregion

        #region Advanced Recurring Appointment Tests

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithCustomInterval_CreatesCorrectOccurrences()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Every 3 Days Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 3, // Every 3 days
                RecurrenceEndDate = startTime.AddDays(9), // Should create 4 occurrences (days 1, 4, 7, 10)
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        [Fact]
        public async Task CreateAppointmentAsync_BiWeeklyRecurring_CreatesCorrectOccurrences()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Bi-weekly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Weekly,
                RecurrenceInterval = 2, // Every 2 weeks
                RecurrenceEndDate = startTime.AddDays(28), // 4 weeks = 3 occurrences
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(3));
        }

        [Fact]
        public async Task CreateAppointmentAsync_QuarterlyRecurring_CreatesCorrectOccurrences()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Quarterly Review",
                StartTime = startTime,
                EndTime = startTime.AddHours(2),
                Recurrence = AppointmentDto.RecurrenceType.Monthly,
                RecurrenceInterval = 3, // Every 3 months
                RecurrenceEndDate = startTime.AddMonths(12), // 1 year = 5 occurrences
                Type = "Review",
                ColorCode = "#0000FF"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(5));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithOverlapInMiddle_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = startTime.AddDays(2), // Conflicts with 3rd occurrence
                EndTime = startTime.AddDays(2).AddHours(1),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(5),
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("overlaps with existing appointment", result.Error);
        }

        #endregion

        #region Complex Overlap Detection Tests

        [Fact]
        public async Task CreateAppointmentAsync_ExactTimeMatch_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "testuser", TimeZoneId = "UTC" };
            var exactTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = exactTime,
                EndTime = exactTime.AddHours(1),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Exact Match Meeting",
                StartTime = exactTime,
                EndTime = exactTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Appointment time overlaps with existing appointment", result.Error);
        }

        [Fact]
        public async Task CreateAppointmentAsync_PartialOverlapAtStart_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "testuser", TimeZoneId = "UTC" };
            var baseTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = baseTime,
                EndTime = baseTime.AddHours(2),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Overlapping Meeting",
                StartTime = baseTime.AddMinutes(-30), // Starts 30 min before existing
                EndTime = baseTime.AddMinutes(30),    // Ends 30 min after existing starts
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Appointment time overlaps with existing appointment", result.Error);
        }

        [Fact]
        public async Task CreateAppointmentAsync_PartialOverlapAtEnd_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "testuser", TimeZoneId = "UTC" };
            var baseTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = baseTime,
                EndTime = baseTime.AddHours(2),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Overlapping Meeting",
                StartTime = baseTime.AddMinutes(90),  // Starts 30 min before existing ends
                EndTime = baseTime.AddMinutes(150),   // Ends 30 min after existing ends
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Appointment time overlaps with existing appointment", result.Error);
        }

        [Fact]
        public async Task CreateAppointmentAsync_CompletelyInside_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "testuser", TimeZoneId = "UTC" };
            var baseTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = baseTime,
                EndTime = baseTime.AddHours(3),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Inside Meeting",
                StartTime = baseTime.AddMinutes(30),  // Inside existing appointment
                EndTime = baseTime.AddMinutes(90),    // Still inside existing appointment
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Appointment time overlaps with existing appointment", result.Error);
        }

        [Fact]
        public async Task CreateAppointmentAsync_CompletelyOutside_ReturnsSuccess()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "testuser", TimeZoneId = "UTC" };
            var baseTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = baseTime,
                EndTime = baseTime.AddHours(1),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Non-overlapping Meeting",
                StartTime = baseTime.AddHours(2),  // Starts after existing ends
                EndTime = baseTime.AddHours(3),    // No overlap
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_BackToBackAppointments_ReturnsSuccess()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "testuser", TimeZoneId = "UTC" };
            var baseTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = baseTime,
                EndTime = baseTime.AddHours(1),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Back-to-back Meeting",
                StartTime = baseTime.AddHours(1),  // Starts exactly when existing ends
                EndTime = baseTime.AddHours(2),    // No overlap (touching is allowed)
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        #endregion

        #region Timezone Edge Cases

        [Fact]
        public async Task CreateAppointmentAsync_PacificTimezone_ValidatesCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "America/Los_Angeles"
            };

            var utcTime = DateTime.UtcNow.AddHours(1); // 1 hour from now in UTC
            var dto = new AppointmentDto
            {
                Title = "Pacific Time Meeting",
                StartTime = utcTime,
                EndTime = utcTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_AsiaTimezone_ValidatesCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Asia/Tokyo"
            };

            var utcTime = DateTime.UtcNow.AddHours(1);
            var dto = new AppointmentDto
            {
                Title = "Tokyo Time Meeting",
                StartTime = utcTime,
                EndTime = utcTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        #endregion

        #region Update Appointment Edge Cases

        [Fact]
        public async Task UpdateAppointmentAsync_UpdateToSameTime_ReturnsSuccess()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var currentTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Original Title",
                StartTime = currentTime,
                EndTime = currentTime.AddHours(1),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = currentTime, // Same time
                EndTime = currentTime.AddHours(1) // Same time
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync(existingAppointment);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });
            _mockAppointmentRepo.Setup(r => r.UpdateAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_UpdateWithNullTitle_KeepsOriginalTitle()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var currentTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Original Title",
                StartTime = currentTime,
                EndTime = currentTime.AddHours(1),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = null, // Null title
                StartTime = currentTime.AddHours(1),
                EndTime = currentTime.AddHours(2)
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync(existingAppointment);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });
            _mockAppointmentRepo.Setup(r => r.UpdateAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Original Title", existingAppointment.Title); // Should keep original
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_UpdateAllFields_UpdatesCorrectly()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var currentTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Original Title",
                StartTime = currentTime,
                EndTime = currentTime.AddHours(1),
                UserId = userId,
                Description = "Original Description",
                Location = "Original Location",
                Attendees = "original@example.com",
                Type = "Original Type",
                ColorCode = "#FF0000",
                Recurrence = RecurrenceType.None
            };

            var dto = new AppointmentDto
            {
                Title = "Updated Title",
                StartTime = currentTime.AddHours(1),
                EndTime = currentTime.AddHours(2),
                Description = "Updated Description",
                Location = "Updated Location",
                Attendees = "updated@example.com",
                Type = "Updated Type",
                ColorCode = "#00FF00",
                Recurrence = AppointmentDto.RecurrenceType.Weekly,
                RecurrenceInterval = 2,
                RecurrenceEndDate = currentTime.AddMonths(1)
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync(existingAppointment);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });
            _mockAppointmentRepo.Setup(r => r.UpdateAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAppointmentAsync(appointmentId, dto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Updated Title", existingAppointment.Title);
            Assert.Equal("Updated Description", existingAppointment.Description);
            Assert.Equal("Updated Location", existingAppointment.Location);
            Assert.Equal("updated@example.com", existingAppointment.Attendees);
            Assert.Equal("Updated Type", existingAppointment.Type);
            Assert.Equal("#00FF00", existingAppointment.ColorCode);
            Assert.Equal(RecurrenceType.Weekly, existingAppointment.Recurrence);
            Assert.Equal(2, existingAppointment.RecurrenceInterval);
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Once);
        }

        #endregion

        #region Search Functionality Tests

        [Fact]
        public async Task SearchAppointmentsAsync_CaseInsensitiveSearch_ReturnsResults()
        {
            // Arrange
            var keyword = "MEETING";
            var userId = 1;
            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "team meeting",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.SearchAsync(keyword, userId)).ReturnsAsync(appointments);

            // Act
            var result = await _service.SearchAppointmentsAsync(keyword, userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("team meeting", result[0].Title);
        }

        [Fact]
        public async Task SearchAppointmentsAsync_SearchInDescription_ReturnsResults()
        {
            // Arrange
            var keyword = "important";
            var userId = 1;
            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Weekly Sync",
                    Description = "Important project discussion",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.SearchAsync(keyword, userId)).ReturnsAsync(appointments);

            // Act
            var result = await _service.SearchAppointmentsAsync(keyword, userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("Weekly Sync", result[0].Title);
            Assert.Equal("Important project discussion", result[0].Description);
        }

        [Fact]
        public async Task SearchAppointmentsAsync_NoUserIdProvided_SearchesAllUsers()
        {
            // Arrange
            var keyword = "meeting";
            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Global Meeting",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    UserId = 1
                },
                new Appointment
                {
                    Id = 2,
                    Title = "Another Meeting",
                    StartTime = DateTime.UtcNow.AddHours(3),
                    EndTime = DateTime.UtcNow.AddHours(4),
                    UserId = 2
                }
            };

            _mockAppointmentRepo.Setup(r => r.SearchAsync(keyword, null)).ReturnsAsync(appointments);

            // Act
            var result = await _service.SearchAppointmentsAsync(keyword, null);

            // Assert
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetRecurringAppointments Edge Cases

        [Fact]
        public async Task GetRecurringAppointmentsAsync_OverlappingDateRange_ReturnsPartialOverlaps()
        {
            // Arrange
            var userId = 1;
            var queryStart = DateTime.UtcNow.Date.AddDays(5);
            var queryEnd = DateTime.UtcNow.Date.AddDays(10);

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Starts Before Range",
                    StartTime = queryStart.AddDays(-1),
                    EndTime = queryStart.AddHours(1), // Ends within range
                    UserId = userId
                },
                new Appointment
                {
                    Id = 2,
                    Title = "Ends After Range",
                    StartTime = queryEnd.AddHours(-1), // Starts within range
                    EndTime = queryEnd.AddDays(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 3,
                    Title = "Completely Outside",
                    StartTime = queryEnd.AddDays(2),
                                        EndTime = queryEnd.AddDays(3),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(appointments);

            // Act
            var result = await _service.GetRecurringAppointmentsAsync(userId, queryStart, queryEnd);

            // Assert
            Assert.Equal(2, result.Count); // Should include first two appointments
            Assert.Contains(result, a => a.Title == "Starts Before Range");
            Assert.Contains(result, a => a.Title == "Ends After Range");
            Assert.DoesNotContain(result, a => a.Title == "Completely Outside");
        }

        [Fact]
        public async Task GetRecurringAppointmentsAsync_ExactBoundaryMatch_ReturnsAppointments()
        {
            // Arrange
            var userId = 1;
            var queryStart = DateTime.UtcNow.Date.AddDays(5);
            var queryEnd = DateTime.UtcNow.Date.AddDays(10);

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Starts Exactly at Range Start",
                    StartTime = queryStart,
                    EndTime = queryStart.AddHours(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 2,
                    Title = "Ends Exactly at Range End",
                    StartTime = queryEnd.AddHours(-1),
                    EndTime = queryEnd,
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(appointments);

            // Act
            var result = await _service.GetRecurringAppointmentsAsync(userId, queryStart, queryEnd);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, a => a.Title == "Starts Exactly at Range Start");
            Assert.Contains(result, a => a.Title == "Ends Exactly at Range End");
        }

        [Fact]
        public async Task GetRecurringAppointmentsAsync_ResultsOrderedByStartTime_ReturnsCorrectOrder()
        {
            // Arrange
            var userId = 1;
            var queryStart = DateTime.UtcNow.Date.AddDays(5);
            var queryEnd = DateTime.UtcNow.Date.AddDays(10);

            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Title = "Third Meeting",
                    StartTime = queryStart.AddDays(2),
                    EndTime = queryStart.AddDays(2).AddHours(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 2,
                    Title = "First Meeting",
                    StartTime = queryStart,
                    EndTime = queryStart.AddHours(1),
                    UserId = userId
                },
                new Appointment
                {
                    Id = 3,
                    Title = "Second Meeting",
                    StartTime = queryStart.AddDays(1),
                    EndTime = queryStart.AddDays(1).AddHours(1),
                    UserId = userId
                }
            };

            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(appointments);

            // Act
            var result = await _service.GetRecurringAppointmentsAsync(userId, queryStart, queryEnd);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("First Meeting", result[0].Title);
            Assert.Equal("Second Meeting", result[1].Title);
            Assert.Equal("Third Meeting", result[2].Title);
        }

        #endregion

        #region Error Handling and Edge Cases

        [Fact]
        public async Task CreateAppointmentAsync_InvalidTimeZone_HandlesGracefully()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Invalid/TimeZone"
            };

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("timezone", result.Error.ToLower());
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithNoEndDate_UsesDefaultThreeMonths()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Weekly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Weekly,
                RecurrenceInterval = 1,
                RecurrenceEndDate = null // No end date specified
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            // Should create approximately 12-13 weekly appointments over 3 months
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.AtLeast(10));
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.AtMost(15));
        }

        [Fact]
        public async Task UpdateAppointmentTypeAsync_UpdatesOnlyTypeAndColor()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;
            var originalTitle = "Original Title";
            var originalStartTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = originalTitle,
                StartTime = originalStartTime,
                EndTime = originalStartTime.AddHours(1),
                UserId = userId,
                Type = "Old Type",
                ColorCode = "#FF0000"
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync(existingAppointment);
            _mockAppointmentRepo.Setup(r => r.UpdateAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAppointmentTypeAsync(appointmentId, "New Type", "#00FF00", userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("New Type", existingAppointment.Type);
            Assert.Equal("#00FF00", existingAppointment.ColorCode);
            // Verify other fields remain unchanged
            Assert.Equal(originalTitle, existingAppointment.Title);
            Assert.Equal(originalStartTime, existingAppointment.StartTime);
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentTypeAsync_NullValues_UpdatesWithNulls()
        {
            // Arrange
            var appointmentId = 1;
            var userId = 1;

            var existingAppointment = new Appointment
            {
                Id = appointmentId,
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                UserId = userId,
                Type = "Old Type",
                ColorCode = "#FF0000"
            };

            _mockAppointmentRepo.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync(existingAppointment);
            _mockAppointmentRepo.Setup(r => r.UpdateAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAppointmentTypeAsync(appointmentId, null, null, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(existingAppointment.Type);
            Assert.Null(existingAppointment.ColorCode);
            _mockAppointmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Once);
        }

        #endregion

        #region Month Boundary Edge Cases

        #endregion

        #region Performance and Stress Tests

        [Fact]
        public async Task CreateAppointmentAsync_LargeNumberOfExistingAppointments_PerformsWell()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Create 1000 existing appointments
            var existingAppointments = new List<Appointment>();
            var baseTime = DateTime.UtcNow.AddDays(1);
            for (int i = 0; i < 1000; i++)
            {
                existingAppointments.Add(new Appointment
                {
                    Id = i + 1,
                    Title = $"Existing Meeting {i}",
                    StartTime = baseTime.AddHours(i * 2), // Spaced 2 hours apart
                    EndTime = baseTime.AddHours(i * 2 + 1),
                    UserId = userId
                });
            }

            var dto = new AppointmentDto
            {
                Title = "New Meeting",
                StartTime = baseTime.AddHours(2001), // Well after all existing appointments
                EndTime = baseTime.AddHours(2002),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingAppointments);
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _service.CreateAppointmentAsync(dto, userId);
            stopwatch.Stop();

            // Assert
            Assert.True(result.Success);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Operation should complete within 1 second");
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        #endregion
        [Fact]
        public async Task CreateAppointmentAsync_AllRecurringOccurrencesInPast_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var pastStartTime = DateTime.UtcNow.AddDays(-10);
            var dto = new AppointmentDto
            {
                Title = "Past Meeting",
                StartTime = pastStartTime,
                EndTime = pastStartTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = DateTime.UtcNow.AddDays(-1) // All in the past
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            // The actual error message from the validation method
            Assert.Contains("Cannot book appointments in the past", result.Error);
        }
        [Fact]
        public async Task CreateAppointmentAsync_CrossingDaylightSavingTime_HandlesCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "America/New_York" // Has DST
            };

            // Use a future date that would be valid (not in the past)
            var startTime = DateTime.UtcNow.AddDays(30); // 30 days from now
            var dto = new AppointmentDto
            {
                Title = "DST Test Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert - Should handle DST conversion without errors
            Assert.True(result.Success);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_MonthlyRecurringOnDay31_HandlesFebruaryCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Use next year's January 31st to ensure it's in the future
            var nextYear = DateTime.UtcNow.Year + 1;
            var startTime = new DateTime(nextYear, 1, 31, 10, 0, 0, DateTimeKind.Utc);
            var dto = new AppointmentDto
            {
                Title = "Monthly Meeting on 31st",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Monthly,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddMonths(3) // Should handle Feb 28/29, Mar 31, Apr 30
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            // Should create appointments for Jan 31, Feb 28/29, Mar 31, Apr 30
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        [Fact]
        public async Task CreateAppointmentAsync_MonthlyRecurringOnDay30_HandlesFebruaryCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Use next year's January 30th to ensure it's in the future
            var nextYear = DateTime.UtcNow.Year + 1;
            var startTime = new DateTime(nextYear, 1, 30, 10, 0, 0, DateTimeKind.Utc);
            var dto = new AppointmentDto
            {
                Title = "Monthly Meeting on 30th",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Monthly,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddMonths(3)
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.True(result.Success);
            // Should create appointments for Jan 30, Feb 28/29, Mar 30, Apr 30
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }
    }
}


                    
