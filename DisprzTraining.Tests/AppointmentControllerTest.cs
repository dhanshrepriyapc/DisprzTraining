using DisprzTraining.Controllers;
using DisprzTraining.DTOs;
using DisprzTraining.Models;
using DisprzTraining.Business;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace DisprzTraining.Tests
{
    public class AppointmentsControllerTest
    {
        // Helper: create controller with mocked user claims
        private AppointmentsController CreateControllerWithUser(Mock<IAppointmentService> mockService, int userId = 1, string timeZoneId = "Asia/Calcutta")
        {
            var controller = new AppointmentsController(mockService.Object);

            // Mock HttpContext and User claims
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim("timeZoneId", timeZoneId)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            };

            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        #region CreateUserAppointment Tests

        [Fact]
        public async Task CreateUserAppointment_ValidDto_ReturnsCreatedResult()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 10, 0, 0), DateTimeKind.Unspecified),
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 11, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            var createdAppointment = new Appointment
            {
                Id = 1,
                Title = dto.Title,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Type = dto.Type,
                ColorCode = dto.ColorCode,
                UserId = 1
            };

            mockService.Setup(s => s.CreateAppointmentAsync(It.IsAny<AppointmentDto>(), 1))
                       .ReturnsAsync((true, null, createdAppointment));

            // Act
            var result = await controller.CreateUserAppointment(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.NotNull(createdResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = createdResult.Value!.GetType();
            var idProperty = responseType.GetProperty("Id");
            var titleProperty = responseType.GetProperty("Title");

            Assert.NotNull(idProperty);
            Assert.NotNull(titleProperty);

            var idValue = idProperty.GetValue(createdResult.Value);
            var titleValue = titleProperty.GetValue(createdResult.Value);

            Assert.Equal(1, idValue);
            Assert.Equal("Test Meeting", titleValue);
        }

        [Fact]
        public async Task CreateUserAppointment_ServiceError_ReturnsConflict()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 10, 0, 0), DateTimeKind.Unspecified),
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 11, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            mockService.Setup(s => s.CreateAppointmentAsync(It.IsAny<AppointmentDto>(), 1))
                       .ReturnsAsync((false, "Appointment conflicts with existing appointment", null));

            // Act
            var result = await controller.CreateUserAppointment(dto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = conflictResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(conflictResult.Value);
            Assert.Equal("Appointment conflicts with existing appointment", messageValue);
        }

        [Fact]
        public async Task CreateUserAppointment_InvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 11, 0, 0), DateTimeKind.Unspecified), // Start after end
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 10, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            // Act
            var result = await controller.CreateUserAppointment(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = badRequestResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(badRequestResult.Value);
            Assert.Equal("StartTime must be before EndTime", messageValue);
        }

        #endregion

        #region UpdateUserAppointment Tests

        [Fact]
        public async Task UpdateUserAppointment_ValidUpdate_ReturnsOk()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Updated Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 14, 0, 0), DateTimeKind.Unspecified),
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 15, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#00FF00"
            };

            mockService.Setup(s => s.UpdateAppointmentAsync(1, It.IsAny<AppointmentDto>(), 1))
                       .ReturnsAsync((true, null));

            // Act
            var result = await controller.UpdateUserAppointment(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = okResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(okResult.Value);
            Assert.Equal("Appointment updated successfully", messageValue);
        }

        [Fact]
        public async Task UpdateUserAppointment_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Updated Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 14, 0, 0), DateTimeKind.Unspecified),
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 15, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#00FF00"
            };

            mockService.Setup(s => s.UpdateAppointmentAsync(1, It.IsAny<AppointmentDto>(), 1))
                       .ReturnsAsync((false, "Unauthorized"));

            // Act
            var result = await controller.UpdateUserAppointment(1, dto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = unauthorizedResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(unauthorizedResult.Value);
            Assert.Equal("Cannot edit another user's appointment", messageValue);
        }

        [Fact]
        public async Task UpdateUserAppointment_NotFound_ReturnsNotFound()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Updated Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 14, 0, 0), DateTimeKind.Unspecified),
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 15, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#00FF00"
            };

            mockService.Setup(s => s.UpdateAppointmentAsync(1, It.IsAny<AppointmentDto>(), 1))
                       .ReturnsAsync((false, "Not found"));

            // Act
            var result = await controller.UpdateUserAppointment(1, dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = notFoundResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(notFoundResult.Value);
            Assert.Equal("Appointment not found", messageValue);
        }

        #endregion

        #region DeleteUserAppointment Tests

        [Fact]
        public async Task DeleteUserAppointment_ValidId_ReturnsNoContent()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((true, null));

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUserAppointment_NotFound_ReturnsNotFound()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "Not found"));

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = notFoundResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(notFoundResult.Value);
            Assert.Equal("Appointment not found", messageValue);
        }

        #endregion

        #region GetUserRecurringAppointments Tests

        [Fact]
        public async Task GetUserRecurringAppointments_ValidDateRange_ReturnsAppointments()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var startDate = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Unspecified);
            var endDate = DateTime.SpecifyKind(new DateTime(2024, 1, 31), DateTimeKind.Unspecified);

            var appointments = new List<AppointmentDto>
            {
                new AppointmentDto
                {
                    Id = 1,
                    Title = "Recurring Meeting",
                    StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 10, 0, 0), DateTimeKind.Unspecified),
                    EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 11, 0, 0), DateTimeKind.Unspecified),
                    Type = "Meeting",
                    ColorCode = "#FF0000"
                }
            };

            mockService.Setup(s => s.GetRecurringAppointmentsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                       .ReturnsAsync(appointments);

            // Act
            var result = await controller.GetUserRecurringAppointments(startDate, endDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAppointments = Assert.IsType<List<AppointmentDto>>(okResult.Value);
            Assert.Single(returnedAppointments);
            Assert.Equal("Recurring Meeting", returnedAppointments[0].Title);
        }

        [Fact]
        public async Task GetUserRecurringAppointments_InvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var startDate = DateTime.SpecifyKind(new DateTime(2024, 1, 31), DateTimeKind.Unspecified);
            var endDate = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Unspecified); // End before start

            // Act
            var result = await controller.GetUserRecurringAppointments(startDate, endDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = badRequestResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(badRequestResult.Value);
            Assert.Equal("Start date must be before end date", messageValue);
        }

        #endregion

        #region GetUserAppointments Tests

        [Fact]
        public async Task GetUserAppointments_ReturnsUserAppointments()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var appointments = new List<AppointmentDto>
            {
                new AppointmentDto
                {
                    Id = 1,
                    Title = "Meeting 1",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    Type = "Meeting",
                    ColorCode = "#FF0000"
                },
                new AppointmentDto
                {
                    Id = 2,
                    Title = "Meeting 2",
                    StartTime = DateTime.UtcNow.AddHours(3),
                    EndTime = DateTime.UtcNow.AddHours(4),
                    Type = "Call",
                    ColorCode = "#00FF00"
                }
            };

            mockService.Setup(s => s.GetAppointmentsForUserAsync(1))
                       .ReturnsAsync(appointments);

            // Act
            var result = await controller.GetUserAppointments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAppointments = Assert.IsType<List<AppointmentDto>>(okResult.Value);
            Assert.Equal(2, returnedAppointments.Count);
            Assert.Equal("Meeting 1", returnedAppointments[0].Title);
            Assert.Equal("Meeting 2", returnedAppointments[1].Title);
        }

        #endregion

        #region SearchUserAppointments Tests

        [Fact]
        public async Task SearchUserAppointments_ValidKeyword_ReturnsMatchingAppointments()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var searchResults = new List<AppointmentDto>
            {
                new AppointmentDto
                {
                    Id = 1,
                    Title = "Team Meeting",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    Type = "Meeting",
                    ColorCode = "#FF0000"
                }
            };

            mockService.Setup(s => s.SearchAppointmentsAsync("meeting", 1))
                       .ReturnsAsync(searchResults);

            // Act
            var result = await controller.SearchUserAppointments("meeting");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAppointments = Assert.IsType<List<AppointmentDto>>(okResult.Value);
            Assert.Single(returnedAppointments);
            Assert.Equal("Team Meeting", returnedAppointments[0].Title);
        }

        #endregion

        #region UpdateTypeAndColor Tests

        [Fact]
        public async Task UpdateTypeAndColor_ValidUpdate_ReturnsOk()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Type = "Call",
                ColorCode = "#0000FF"
            };

            mockService.Setup(s => s.UpdateAppointmentTypeAsync(1, "Call", "#0000FF", 1))
                       .ReturnsAsync((true, null));

            // Act
            var result = await controller.UpdateTypeAndColor(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Use reflection to access anonymous object properties
            var responseType = okResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            Assert.NotNull(messageProperty);

            var messageValue = messageProperty.GetValue(okResult.Value);
            Assert.Equal("Appointment type/color updated successfully", messageValue);
        }

        #endregion
        // Add these tests to your existing AppointmentsControllerTest class

        #region GetUserAppointmentById Tests
        [Fact]
        public async Task GetUserAppointmentById_ValidId_ReturnsAppointment()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var appointment = new Appointment
            {
                Id = 1,
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Type = "Meeting",
                ColorCode = "#FF0000",
                UserId = 1
            };

            mockService.Setup(s => s.GetAppointmentByIdAsync(1))
                    .ReturnsAsync(appointment);

            // Act
            var result = await controller.GetUserAppointmentById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedDto = Assert.IsType<AppointmentDto>(okResult.Value);
            Assert.Equal("Test Meeting", returnedDto.Title);
            Assert.Equal(1, returnedDto.Id);
        }

        [Fact]
        public async Task GetUserAppointmentById_AppointmentNotFound_ReturnsNotFound()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.GetAppointmentByIdAsync(1))
                    .ReturnsAsync((Appointment?)null);

            // Act
            var result = await controller.GetUserAppointmentById(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var responseType = notFoundResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(notFoundResult.Value);
            Assert.Equal("Appointment not found", messageValue);
        }

        [Fact]
        public async Task GetUserAppointmentById_WrongUser_ReturnsNotFound()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1); // User ID 1

            var appointment = new Appointment
            {
                Id = 1,
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Type = "Meeting",
                ColorCode = "#FF0000",
                UserId = 2 // Different user ID
            };

            mockService.Setup(s => s.GetAppointmentByIdAsync(1))
                    .ReturnsAsync(appointment);

            // Act
            var result = await controller.GetUserAppointmentById(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var responseType = notFoundResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(notFoundResult.Value);
            Assert.Equal("Appointment not found", messageValue);
        }
        #endregion

        #region Additional CreateUserAppointment Tests
        [Fact]
        public async Task CreateUserAppointment_NullAppointmentReturned_ReturnsInternalServerError()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Test Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 10, 0, 0), DateTimeKind.Unspecified),
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 11, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#FF0000"
            };

            mockService.Setup(s => s.CreateAppointmentAsync(It.IsAny<AppointmentDto>(), 1))
                    .ReturnsAsync((true, null, (Appointment?)null)); // Success but null appointment

            // Act
            var result = await controller.CreateUserAppointment(dto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var responseType = statusCodeResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(statusCodeResult.Value);
            Assert.Equal("Failed to create appointment", messageValue);
        }
        #endregion

        #region Additional UpdateUserAppointment Tests
        [Fact]
        public async Task UpdateUserAppointment_InvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Updated Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 15, 0, 0), DateTimeKind.Unspecified), // Start after end
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 14, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#00FF00"
            };

            // Act
            var result = await controller.UpdateUserAppointment(1, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var responseType = badRequestResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(badRequestResult.Value);
            Assert.Equal("StartTime must be before EndTime", messageValue);
        }

        [Fact]
        public async Task UpdateUserAppointment_OtherError_ReturnsConflict()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Title = "Updated Meeting",
                StartTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 14, 0, 0), DateTimeKind.Unspecified),
                EndTime = DateTime.SpecifyKind(new DateTime(2024, 1, 15, 15, 0, 0), DateTimeKind.Unspecified),
                Type = "Meeting",
                ColorCode = "#00FF00"
            };

            mockService.Setup(s => s.UpdateAppointmentAsync(1, It.IsAny<AppointmentDto>(), 1))
                    .ReturnsAsync((false, "Time slot conflicts with another appointment"));

            // Act
            var result = await controller.UpdateUserAppointment(1, dto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var responseType = conflictResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(conflictResult.Value);
            Assert.Equal("Time slot conflicts with another appointment", messageValue);
        }
        #endregion

        #region Additional DeleteUserAppointment Tests
        [Fact]
        public async Task DeleteUserAppointment_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                    .ReturnsAsync((false, "Unauthorized"));

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var responseType = unauthorizedResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(unauthorizedResult.Value);
            Assert.Equal("Cannot delete another user's appointment", messageValue);
        }
        #endregion

        #region Additional UpdateTypeAndColor Tests
        [Fact]
        public async Task UpdateTypeAndColor_NotFound_ReturnsNotFound()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Type = "Call",
                ColorCode = "#0000FF"
            };

            mockService.Setup(s => s.UpdateAppointmentTypeAsync(1, "Call", "#0000FF", 1))
                    .ReturnsAsync((false, "Not found"));

            // Act
            var result = await controller.UpdateTypeAndColor(1, dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var responseType = notFoundResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(notFoundResult.Value);
            Assert.Equal("Appointment not found", messageValue);
        }

        [Fact]
        public async Task UpdateTypeAndColor_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Type = "Call",
                ColorCode = "#0000FF"
            };

            mockService.Setup(s => s.UpdateAppointmentTypeAsync(1, "Call", "#0000FF", 1))
                    .ReturnsAsync((false, "Unauthorized"));

            // Act
            var result = await controller.UpdateTypeAndColor(1, dto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var responseType = unauthorizedResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(unauthorizedResult.Value);
            Assert.Equal("Cannot edit another user's appointment", messageValue);
        }

        [Fact]
        public async Task UpdateTypeAndColor_OtherError_ReturnsConflict()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            var dto = new AppointmentDto
            {
                Type = "Call",
                ColorCode = "#0000FF"
            };

            mockService.Setup(s => s.UpdateAppointmentTypeAsync(1, "Call", "#0000FF", 1))
                    .ReturnsAsync((false, "Some other error"));

            // Act
            var result = await controller.UpdateTypeAndColor(1, dto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var responseType = conflictResult.Value!.GetType();
            var messageProperty = responseType.GetProperty("message");
            var messageValue = messageProperty!.GetValue(conflictResult.Value);
            Assert.Equal("Some other error", messageValue);
        }
        #endregion

        #region TimeZone Helper Tests
        [Fact]
        public async Task GetUserAppointments_WithoutTimeZoneClaim_DefaultsToUTC()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = new AppointmentsController(mockService.Object);

            // Create controller with claims but no timeZoneId
            var claims = new List<Claim>
            {
                new Claim("id", "1")
                // No timeZoneId claim
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            };
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpContext
            };

            var appointments = new List<AppointmentDto>
            {
                new AppointmentDto
                {
                    Id = 1,
                    Title = "Test Meeting",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    Type = "Meeting",
                    ColorCode = "#FF0000"
                }
            };

            mockService.Setup(s => s.GetAppointmentsForUserAsync(1))
                    .ReturnsAsync(appointments);

            // Act
            var result = await controller.GetUserAppointments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAppointments = Assert.IsType<List<AppointmentDto>>(okResult.Value);
            Assert.Single(returnedAppointments);
        }
        #endregion
        #region Additional DeleteUserAppointment Tests - Complete Coverage

        [Fact]
        public async Task DeleteUserAppointment_ServiceReturnsSuccessFalseWithOtherError_ReturnsNoContent()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "Some database error")); // Not "Not found" or "Unauthorized"

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            // Based on the controller code, if error is not "Not found" or "Unauthorized", it falls through to return NoContent
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_ServiceReturnsSuccessFalseWithNullError_ReturnsNoContent()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, null)); // Null error

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            // Null error doesn't match "Not found" or "Unauthorized", so falls through to NoContent
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_ServiceReturnsSuccessFalseWithEmptyError_ReturnsNoContent()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "")); // Empty string error

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            // Empty string doesn't match "Not found" or "Unauthorized", so falls through to NoContent
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_ZeroId_CallsServiceWithZero()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(0, 1))
                       .ReturnsAsync((false, "Not found"));

            // Act
            var result = await controller.DeleteUserAppointment(0);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(0, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_NegativeId_CallsServiceWithNegativeId()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(-5, 1))
                       .ReturnsAsync((false, "Not found"));

            // Act
            var result = await controller.DeleteUserAppointment(-5);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(-5, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_LargeId_HandlesCorrectly()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(int.MaxValue, 1))
                       .ReturnsAsync((true, null));

            // Act
            var result = await controller.DeleteUserAppointment(int.MaxValue);

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(int.MaxValue, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_ServiceThrowsException_ExceptionPropagates()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                controller.DeleteUserAppointment(1));

            Assert.Equal("Database connection failed", exception.Message);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_DifferentUserId_ExtractsCorrectUserIdFromClaims()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var expectedUserId = 999;
            var controller = CreateControllerWithUser(mockService, expectedUserId); // Different user ID

            mockService.Setup(s => s.DeleteAppointmentAsync(1, expectedUserId))
                       .ReturnsAsync((true, null));

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, expectedUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_CaseSensitiveErrorMessages_HandlesCorrectly()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            // Test case sensitivity - "not found" vs "Not found"
            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "not found")); // lowercase

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            // Should fall through to NoContent because it's case-sensitive match
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_CaseSensitiveUnauthorized_HandlesCorrectly()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            // Test case sensitivity - "unauthorized" vs "Unauthorized"
            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "unauthorized")); // lowercase

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            // Should fall through to NoContent because it's case-sensitive match
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_ErrorMessageWithExtraSpaces_HandlesCorrectly()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, " Not found ")); // Extra spaces

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            // Should fall through to NoContent because exact match is required
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAppointment_PartialErrorMatch_HandlesCorrectly()
        {
            // Arrange
            var mockService = new Mock<IAppointmentService>();
            var controller = CreateControllerWithUser(mockService, 1);

            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "Appointment Not found in database")); // Contains "Not found" but not exact

            // Act
            var result = await controller.DeleteUserAppointment(1);

            // Assert
            // Should fall through to NoContent because exact match is required
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteAppointmentAsync(1, 1), Times.Once);
        }

        #endregion




    }
}
