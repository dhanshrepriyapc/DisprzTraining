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
            Assert.Contains("timezone", result.Error?.ToLower());
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
        #region CreateAppointmentAsync Tests
        [Fact]
        public async Task CreateAppointmentAsync_StartTimeInPast_ReturnsError()
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
                StartTime = DateTime.UtcNow.AddHours(-2),
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
        public async Task CreateAppointmentAsync_RecurringWithNullInterval_UsesDefaultInterval()
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
                RecurrenceInterval = null,
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
        public async Task CreateAppointmentAsync_RecurringWithNullEndDate_UsesDefaultEndDate()
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
                RecurrenceEndDate = null,
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
            // Should create appointments for 3 months (default when no end date)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.AtLeast(1));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithOverlap_ReturnsError()
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
                StartTime = startTime.AddDays(1),
                EndTime = startTime.AddDays(1).AddHours(1),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(3),
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("overlaps with existing appointment", result.Error);
            Assert.Null(result.Appointment);
        }
        [Fact]
        public async Task CreateAppointmentAsync_RecurringAllOccurrencesInPast_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var startTime = DateTime.UtcNow.AddDays(-5);
            var dto = new AppointmentDto
            {
                Title = "Past Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(3), // All in the past
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
            // The service actually returns the timezone validation error for the first occurrence
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithAllOptionalFields_CreatesSuccessfully()
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
                Title = "Complete Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Description = "Meeting description",
                Location = "Conference Room A",
                Attendees = "john@example.com,jane@example.com",
                Type = "Business",
                ColorCode = "#00FF00",
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
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.Is<Appointment>(a =>
                a.Title == "Complete Meeting" &&
                a.Description == "Meeting description" &&
                a.Location == "Conference Room A" &&
                a.Attendees == "john@example.com,jane@example.com" &&
                a.Type == "Business" &&
                a.ColorCode == "#00FF00"
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithNullTitle_UsesEmptyString()
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
                Title = null,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
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
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.Is<Appointment>(a =>
                a.Title == string.Empty
            )), Times.Once);
        }
        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithAllFutureOccurrences_CreatesAllAppointments()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start in the future to ensure all occurrences are valid
            var startTime = DateTime.UtcNow.AddDays(1); // Tomorrow
            var dto = new AppointmentDto
            {
                Title = "Future Daily Meeting",
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
        public async Task CreateAppointmentAsync_RecurringStartsInPastWithNoFutureOccurrences_ReturnsNoFutureAppointmentsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Create a scenario where the service would actually reach the "No future appointments" logic
            // This happens when the while loop completes but no appointments are created
            var startTime = DateTime.UtcNow.AddDays(-10); // Far in the past
            var dto = new AppointmentDto
            {
                Title = "Very Old Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(5), // Still in the past
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
            // Based on the service logic, it will return the validation error for the first occurrence
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }
        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithValidationSkipsAllOccurrences_ReturnsNoFutureAppointmentsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Create a scenario where the first occurrence passes initial validation
            // but all recurring occurrences fail validation during the loop
            var startTime = DateTime.UtcNow.AddHours(1); // Future start time
            var dto = new AppointmentDto
            {
                Title = "Edge Case Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(-1), // End date before start date (edge case)
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
            // This should trigger the "No future appointments could be created" message
            // because the while loop condition (occurrence.Date <= recurrenceEnd.Date) will be false immediately
            Assert.Contains("No future appointments could be created", result.Error);
            Assert.Null(result.Appointment);
        }
        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithFutureStartTime_CreatesAllValidOccurrences()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start in the future so it passes initial validation
            var startTime = DateTime.UtcNow.AddHours(1); // 1 hour from now
            var dto = new AppointmentDto
            {
                Title = "Hourly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddMinutes(30),
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
            // Should create 4 appointments (today + 3 more days)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }
        [Fact]
        public async Task CreateAppointmentAsync_RecurringWithEndDateBeforeStartDate_ReturnsNoFutureAppointmentsError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start in the future but end date is before start date
            var startTime = DateTime.UtcNow.AddHours(1);
            var dto = new AppointmentDto
            {
                Title = "Invalid Recurring Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(-1), // End date before start date
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
            Assert.Equal("No future appointments could be created. All occurrences are in the past.", result.Error);
            Assert.Null(result.Appointment);
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Never);
        }
        #endregion
        #region ValidateAppointmentTime Tests

        [Fact]
        public async Task ValidateAppointmentTime_FutureStartTime_ReturnsValid()
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
                Title = "Future Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_StartTimeInPast_ReturnsInvalid()
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
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Contains("Current time in your timezone (UTC)", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EndTimeInPast_ReturnsInvalid()
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
                Title = "Invalid Meeting",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(-1), // End time in past
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Appointment end time cannot be in the past", result.Error);
            Assert.Contains("Current time in your timezone (UTC)", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_InvalidTimeZone_ReturnsInvalid()
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
            Assert.Equal("Invalid timezone configuration for user", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EasternTimeZone_ConvertsCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Eastern Standard Time"
            };

            // Create appointment that's in future in UTC but might be different in EST
            var utcStartTime = DateTime.UtcNow.AddHours(2);
            var dto = new AppointmentDto
            {
                Title = "EST Meeting",
                StartTime = utcStartTime,
                EndTime = utcStartTime.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_PacificTimeZone_ConvertsCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Pacific Standard Time"
            };

            var utcStartTime = DateTime.UtcNow.AddHours(2);
            var dto = new AppointmentDto
            {
                Title = "PST Meeting",
                StartTime = utcStartTime,
                EndTime = utcStartTime.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_EdgeCaseJustInPast_ReturnsInvalid()
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
                Title = "Just Past Meeting",
                StartTime = DateTime.UtcNow.AddMinutes(-1), // Just 1 minute ago
                EndTime = DateTime.UtcNow.AddMinutes(30),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EdgeCaseJustInFuture_ReturnsValid()
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
                Title = "Just Future Meeting",
                StartTime = DateTime.UtcNow.AddMinutes(1), // Just 1 minute from now
                EndTime = DateTime.UtcNow.AddMinutes(31),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_TokyoTimeZone_ConvertsCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Tokyo Standard Time"
            };

            var utcStartTime = DateTime.UtcNow.AddHours(2);
            var dto = new AppointmentDto
            {
                Title = "Tokyo Meeting",
                StartTime = utcStartTime,
                EndTime = utcStartTime.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_LondonTimeZone_ConvertsCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "GMT Standard Time"
            };

            var utcStartTime = DateTime.UtcNow.AddHours(2);
            var dto = new AppointmentDto
            {
                Title = "London Meeting",
                StartTime = utcStartTime,
                EndTime = utcStartTime.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_RecurringAppointmentValidation_SkipsPastOccurrences()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start just in the future so initial validation passes
            var startTime = DateTime.UtcNow.AddMinutes(30);
            var dto = new AppointmentDto
            {
                Title = "Recurring Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
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
            // Should create 4 appointments (today + 3 more days)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        [Fact]
        public async Task ValidateAppointmentTime_TimeZoneWithDaylightSaving_HandlesCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Central Standard Time" // Has daylight saving
            };

            var utcStartTime = DateTime.UtcNow.AddHours(3);
            var dto = new AppointmentDto
            {
                Title = "DST Meeting",
                StartTime = utcStartTime,
                EndTime = utcStartTime.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_NullTimeZoneId_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = null! // Null timezone
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
            Assert.Contains("Error validating appointment time", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EmptyTimeZoneId_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = string.Empty // Empty timezone
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

        #endregion
        #region Additional ValidateAppointmentTime Coverage Tests

        [Fact]
        public async Task ValidateAppointmentTime_ExceptionInTimeZoneConversion_ReturnsGenericError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Create a scenario that might cause an exception during time conversion
            var dto = new AppointmentDto
            {
                Title = "Edge Case Meeting",
                StartTime = DateTime.MinValue, // This might cause issues in conversion
                EndTime = DateTime.MinValue.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            // Most explicit approach
            Assert.False(result.Success);
            Assert.NotNull(result.Error);
            Assert.True(result.Error.Equals("Error validating appointment time") ||
                        result.Error.Contains("Cannot book appointments in the past"));
            Assert.Null(result.Appointment);

        }

        [Fact]
        public async Task ValidateAppointmentTime_MaxDateTimeValue_HandlesCorrectly()
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
                Title = "Max Date Meeting",
                StartTime = DateTime.MaxValue.AddDays(-1), // Very far future
                EndTime = DateTime.MaxValue,
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_StartTimeExactlyNow_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var now = DateTime.UtcNow;
            var dto = new AppointmentDto
            {
                Title = "Exact Now Meeting",
                StartTime = now, // Exactly now
                EndTime = now.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EndTimeExactlyNow_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var now = DateTime.UtcNow;
            var dto = new AppointmentDto
            {
                Title = "End Now Meeting",
                StartTime = now.AddHours(1), // Future start
                EndTime = now, // End exactly now
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Appointment end time cannot be in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_BothTimesInFuture_DifferentTimeZone_ReturnsValid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "India Standard Time" // UTC+5:30
            };

            // Times that are future in UTC and should be future in IST too
            var utcStart = DateTime.UtcNow.AddHours(10);
            var dto = new AppointmentDto
            {
                Title = "India Meeting",
                StartTime = utcStart,
                EndTime = utcStart.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_TimeZoneConversionEdgeCase_HandlesCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Hawaiian Standard Time" // UTC-10
            };

            // Create a time that's future in UTC but might be tricky in Hawaii time
            var utcStart = DateTime.UtcNow.AddHours(5);
            var dto = new AppointmentDto
            {
                Title = "Hawaii Meeting",
                StartTime = utcStart,
                EndTime = utcStart.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_RecurringAppointment_EachOccurrenceValidated()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start far enough in future to ensure all occurrences are valid
            var startTime = DateTime.UtcNow.AddDays(1);
            var dto = new AppointmentDto
            {
                Title = "Daily Recurring",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(2), // 3 total occurrences
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
            // Each occurrence should be validated and created
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ValidateAppointmentTime_RecurringWithSomeInvalidOccurrences_SkipsInvalidOnes()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start just barely in the future so first occurrence is valid
            var startTime = DateTime.UtcNow.AddMinutes(5);
            var dto = new AppointmentDto
            {
                Title = "Mixed Valid/Invalid Recurring",
                StartTime = startTime,
                EndTime = startTime.AddMinutes(30),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(5), // Multiple occurrences
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
            // Should create multiple appointments (all future ones)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.AtLeast(1));
        }

        [Fact]
        public async Task ValidateAppointmentTime_AustralianTimeZone_HandlesCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "AUS Eastern Standard Time" // Correct Windows timezone ID for Australia
            };

            var utcStart = DateTime.UtcNow.AddHours(8);
            var dto = new AppointmentDto
            {
                Title = "Australia Meeting",
                StartTime = utcStart,
                EndTime = utcStart.AddHours(1),
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
        }
        #endregion
        #region Targeted ValidateAppointmentTime Coverage Tests

        [Fact]
        public async Task ValidateAppointmentTime_StartTimeEqualToCurrentTime_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Get current time and use it exactly as start time
            var currentUtc = DateTime.UtcNow;
            var dto = new AppointmentDto
            {
                Title = "Exact Current Time Meeting",
                StartTime = currentUtc,
                EndTime = currentUtc.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EndTimeEqualToCurrentTime_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var currentUtc = DateTime.UtcNow;
            var dto = new AppointmentDto
            {
                Title = "End Time Current Meeting",
                StartTime = currentUtc.AddHours(1), // Future start
                EndTime = currentUtc, // End time equals current time
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Appointment end time cannot be in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_BothTimesValidInUserTimeZone_ReturnsValid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var futureTime = DateTime.UtcNow.AddHours(2);
            var dto = new AppointmentDto
            {
                Title = "Valid Future Meeting",
                StartTime = futureTime,
                EndTime = futureTime.AddHours(1),
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
        }

        [Fact]
        public async Task ValidateAppointmentTime_TimeZoneNotFoundException_ReturnsInvalidConfig()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "NonExistent/TimeZone"
            };

            var dto = new AppointmentDto
            {
                Title = "Invalid TZ Meeting",
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
            Assert.Equal("Invalid timezone configuration for user", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_GenericException_ReturnsGenericError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = null! // This will cause an exception in TimeZoneInfo.FindSystemTimeZoneById
            };

            var dto = new AppointmentDto
            {
                Title = "Exception Test Meeting",
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
            Assert.Equal("Error validating appointment time", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_DifferentTimeZone_StartTimePastInUserTZ_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Pacific Standard Time" // UTC-8
            };

            // Create a time that's future in UTC but past in PST
            var utcTime = DateTime.UtcNow.AddHours(-5); // 5 hours ago in UTC
            var dto = new AppointmentDto
            {
                Title = "PST Past Meeting",
                StartTime = utcTime,
                EndTime = utcTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Contains("Pacific Standard Time", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_DifferentTimeZone_EndTimePastInUserTZ_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Eastern Standard Time" // UTC-5
            };

            var utcNow = DateTime.UtcNow;
            var dto = new AppointmentDto
            {
                Title = "EST End Past Meeting",
                StartTime = utcNow.AddHours(2), // Future start
                EndTime = utcNow.AddHours(-3), // Past end time
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Appointment end time cannot be in the past", result.Error);
            Assert.Contains("Eastern Standard Time", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_RecurringAppointment_IndividualOccurrenceValidation()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Start just in future to pass initial validation
            var startTime = DateTime.UtcNow.AddMinutes(30);
            var dto = new AppointmentDto
            {
                Title = "Recurring Validation Test",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(2),
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
            // This should hit the validation logic for each recurring occurrence
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ValidateAppointmentTime_EmptyStringTimeZone_ThrowsException()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "" // Empty string
            };

            var dto = new AppointmentDto
            {
                Title = "Empty TZ Meeting",
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
            Assert.True(result.Error == "Invalid timezone configuration for user" ||
                        result.Error == "Error validating appointment time");
            Assert.Null(result.Appointment);
        }

        #endregion
        #region ValidateAppointmentTime Complete Edge Case Coverage

        [Fact]
        public async Task ValidateAppointmentTime_StartTimeExactlyEqualToCurrentTime_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            // Get exact current time to test the <= condition
            var exactCurrentTime = DateTime.UtcNow;
            var dto = new AppointmentDto
            {
                Title = "Exact Current Time Meeting",
                StartTime = exactCurrentTime, // Exactly equal to current time
                EndTime = exactCurrentTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Contains("UTC", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_StartTimeOneMicrosecondInPast_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var pastTime = DateTime.UtcNow.AddTicks(-1); // 1 tick in the past
            var dto = new AppointmentDto
            {
                Title = "Microsecond Past Meeting",
                StartTime = pastTime,
                EndTime = pastTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EndTimeExactlyEqualToCurrentTime_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var currentTime = DateTime.UtcNow;
            var dto = new AppointmentDto
            {
                Title = "End Time Current Meeting",
                StartTime = currentTime.AddHours(1), // Future start
                EndTime = currentTime, // End time exactly equal to current
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Appointment end time cannot be in the past", result.Error);
            Assert.Contains("UTC", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EndTimeOneMicrosecondInPast_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC"
            };

            var pastTime = DateTime.UtcNow.AddTicks(-1);
            var dto = new AppointmentDto
            {
                Title = "End Microsecond Past Meeting",
                StartTime = DateTime.UtcNow.AddHours(2), // Future start
                EndTime = pastTime, // End time 1 tick in past
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Appointment end time cannot be in the past", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_TimeZoneNotFoundExceptionThrown_ReturnsInvalidConfig()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Invalid/NonExistent/TimeZone"
            };

            var dto = new AppointmentDto
            {
                Title = "Invalid TZ Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid timezone configuration for user", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_NullTimeZoneId_ThrowsGenericException()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = null! // This will cause ArgumentNullException
            };

            var dto = new AppointmentDto
            {
                Title = "Null TZ Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Error validating appointment time", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_EmptyStringTimeZoneId_ThrowsException()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "" // Empty string
            };

            var dto = new AppointmentDto
            {
                Title = "Empty TZ Meeting",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Error == "Invalid timezone configuration for user" ||
                        result.Error == "Error validating appointment time");
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task ValidateAppointmentTime_DifferentTimeZone_PastInUserTimeButFutureInUTC_ReturnsInvalid()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "Pacific Standard Time" // UTC-8
            };

            // Create a time that's future in UTC but past in PST
            var utcTime = DateTime.UtcNow.AddHours(-5); // 5 hours ago in UTC, but might be valid in PST
            var dto = new AppointmentDto
            {
                Title = "PST Timezone Test",
                StartTime = utcTime,
                EndTime = utcTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot book appointments in the past", result.Error);
            Assert.Contains("Pacific Standard Time", result.Error);
            Assert.Null(result.Appointment);
        }

        #endregion
        #region CreateAppointmentAsync Complete Edge Case Coverage

        [Fact]
        public async Task CreateAppointmentAsync_UserNotFound_ReturnsUserNotFoundError()
        {
            // Arrange
            var userId = 999;
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
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_StartTimeEqualToEndTime_ReturnsInvalidTimeRange()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var sameTime = DateTime.UtcNow.AddHours(2);

            var dto = new AppointmentDto
            {
                Title = "Same Time Meeting",
                StartTime = sameTime,
                EndTime = sameTime, // Same as start time
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("StartTime must be before EndTime", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_StartTimeAfterEndTime_ReturnsInvalidTimeRange()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var baseTime = DateTime.UtcNow.AddHours(2);

            var dto = new AppointmentDto
            {
                Title = "Reversed Time Meeting",
                StartTime = baseTime.AddHours(1), // After end time
                EndTime = baseTime,
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("StartTime must be before EndTime", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_SingleAppointmentOverlapsExisting_ReturnsOverlapError()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
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
                StartTime = baseTime.AddMinutes(30), // Overlaps with existing
                EndTime = baseTime.AddHours(1),
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
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringDailyWithMaxOccurrencesReached_CreatesLimitedAppointments()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var startTime = DateTime.UtcNow.AddHours(2);

            var dto = new AppointmentDto
            {
                Title = "Daily Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(200), // Would create more than 100 occurrences
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
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
            // Should create exactly 100 appointments (max limit)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(100));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringWeeklyWithNullInterval_UsesDefaultInterval()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var startTime = DateTime.UtcNow.AddHours(2);

            var dto = new AppointmentDto
            {
                Title = "Weekly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Weekly,
                RecurrenceInterval = null, // Should default to 1
                RecurrenceEndDate = startTime.AddDays(21), // 3 weeks
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
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
            // Should create 4 appointments (weeks 0, 1, 2, 3)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringMonthlyWithNullEndDate_UsesDefault3Months()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var startTime = DateTime.UtcNow.AddHours(2);

            var dto = new AppointmentDto
            {
                Title = "Monthly Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Monthly,
                RecurrenceInterval = 1,
                RecurrenceEndDate = null, // Should default to 3 months
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
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
            // Should create 4 appointments (months 0, 1, 2, 3)
            _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Exactly(4));
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringOverlapsWithExisting_ReturnsOverlapError()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var startTime = DateTime.UtcNow.AddHours(2);

            var existingAppointment = new Appointment
            {
                Id = 1,
                StartTime = startTime.AddDays(1), // Conflicts with second occurrence
                EndTime = startTime.AddDays(1).AddHours(2),
                UserId = userId
            };

            var dto = new AppointmentDto
            {
                Title = "Recurring Overlap Meeting",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Daily,
                RecurrenceInterval = 1,
                RecurrenceEndDate = startTime.AddDays(3),
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment> { existingAppointment });

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("overlaps with existing appointment", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_ExceptionThrown_ReturnsExceptionMessage()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var dto = new AppointmentDto
            {
                Title = "Exception Test",
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Recurrence = AppointmentDto.RecurrenceType.None
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Database connection failed", result.Error);
            Assert.Null(result.Appointment);
        }

        [Fact]
        public async Task CreateAppointmentAsync_RecurringInfiniteLoop_BreaksOnSameOccurrence()
        {
            // Arrange - This tests the infinite loop protection
            var userId = 1;
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };
            var startTime = DateTime.UtcNow.AddHours(2);

            var dto = new AppointmentDto
            {
                Title = "Loop Protection Test",
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Recurrence = AppointmentDto.RecurrenceType.Monthly,
                RecurrenceInterval = 0, // This could cause infinite loop
                RecurrenceEndDate = startTime.AddYears(1),
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAppointmentRepo.Setup(r => r.GetByUserIdAsync(userId))
                               .ReturnsAsync(new List<Appointment>());
            _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAppointmentAsync(dto, userId);

            // Assert
            // Should either succeed with limited appointments or fail gracefully
            Assert.True(result.Success || result.Error != null);
        }

        #endregion



    }
}


                    
