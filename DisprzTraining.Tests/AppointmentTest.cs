using DisprzTraining.Controllers;
using DisprzTraining.DTOs;
using DisprzTraining.Business;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DisprzTraining.Models;

namespace DisprzTraining.Tests
{
    public class AppointmentsControllerTest
    {
        // -------------------- GET APPOINTMENTS --------------------
        [Fact]
        public async Task GetAppointments_ReturnsBadRequest_WhenUserIdInvalid()
        {
            var mockService = new Mock<IAppointmentService>();
            var controller = new AppointmentsController(mockService.Object);

            var result = await controller.GetAppointments(0);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badResult.StatusCode);
        }

        [Fact]
        public async Task GetAppointments_ReturnsOk_WithAppointments()
        {
            var mockService = new Mock<IAppointmentService>();
            var fakeAppointments = new List<AppointmentDto> 
            { 
                new AppointmentDto { Id = 1, Title = "Meeting" } 
            };
            mockService.Setup(s => s.GetAppointmentsForUserAsync(1))
                       .ReturnsAsync(fakeAppointments);

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.GetAppointments(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<AppointmentDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetAppointments_ReturnsOk_WithEmptyList()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.GetAppointmentsForUserAsync(1))
                       .ReturnsAsync(new List<AppointmentDto>());

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.GetAppointments(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<AppointmentDto>>(okResult.Value);
            Assert.Empty(data);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.GetAppointmentByIdAsync(1))
                       .ReturnsAsync((Appointment?)null);

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.GetById(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithAppointment()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.GetAppointmentByIdAsync(1))
                       .ReturnsAsync(new Appointment
                       {
                           Id = 1,
                           Title = "Call",
                           StartTime = DateTime.Now,
                           EndTime = DateTime.Now.AddHours(1),
                           UserId = 1
                       });

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.GetById(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<AppointmentDto>(okResult.Value);
            Assert.Equal(1, dto.Id);
            Assert.Equal("Call", dto.Title);
        }

        // -------------------- CREATE APPOINTMENT --------------------
        [Fact]
        public async Task CreateAppointment_ReturnsBadRequest_WhenUserIdInvalid()
        {
            var mockService = new Mock<IAppointmentService>();
            var controller = new AppointmentsController(mockService.Object);

            var result = await controller.CreateAppointment(new AppointmentDto(), 0);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badResult.StatusCode);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsCreated_WhenSuccess()
        {
            var mockService = new Mock<IAppointmentService>();
            var dto = new AppointmentDto { Title = "Meeting" };
            mockService.Setup(s => s.CreateAppointmentAsync(dto, 1))
                       .ReturnsAsync((true, null, new Appointment { Id = 1, Title = "Meeting" }));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.CreateAppointment(dto, 1);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var appointment = Assert.IsType<Appointment>(createdResult.Value);
            Assert.Equal(1, appointment.Id);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsConflict_WhenOverlap()
        {
            var mockService = new Mock<IAppointmentService>();
            var dto = new AppointmentDto { Title = "Overlap" };
            mockService.Setup(s => s.CreateAppointmentAsync(dto, 1))
                       .ReturnsAsync((false, "Appointment time overlaps with existing appointment", null!));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.CreateAppointment(dto, 1);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        // -------------------- UPDATE APPOINTMENT --------------------
        [Fact]
        public async Task UpdateAppointment_ReturnsBadRequest_WhenUserIdInvalid()
        {
            var mockService = new Mock<IAppointmentService>();
            var controller = new AppointmentsController(mockService.Object);

            var result = await controller.UpdateAppointment(1, new AppointmentDto(), 0);
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badResult.StatusCode);
        }

        [Fact]
        public async Task UpdateAppointment_ReturnsNotFound_WhenNotExist()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.UpdateAppointmentAsync(1, It.IsAny<AppointmentDto>(), 1))
                       .ReturnsAsync((false, "Not found"));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.UpdateAppointment(1, new AppointmentDto(), 1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateAppointment_ReturnsUnauthorized_WhenUserMismatch()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.UpdateAppointmentAsync(1, It.IsAny<AppointmentDto>(), 1))
                       .ReturnsAsync((false, "Unauthorized"));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.UpdateAppointment(1, new AppointmentDto(), 1);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public async Task UpdateAppointment_ReturnsConflict_WhenOverlap()
        {
            var mockService = new Mock<IAppointmentService>();
            var dto = new AppointmentDto { Title = "UpdateOverlap" };
            mockService.Setup(s => s.UpdateAppointmentAsync(1, dto, 1))
                       .ReturnsAsync((false, "Appointment time overlaps with existing appointment"));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.UpdateAppointment(1, dto, 1);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        // -------------------- DELETE APPOINTMENT --------------------
        [Fact]
        public async Task DeleteAppointment_ReturnsBadRequest_WhenUserIdInvalid()
        {
            var mockService = new Mock<IAppointmentService>();
            var controller = new AppointmentsController(mockService.Object);

            var result = await controller.DeleteAppointment(1, 0);
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badResult.StatusCode);
        }

        [Fact]
        public async Task DeleteAppointment_ReturnsNotFound_WhenNotExist()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "Not found"));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.DeleteAppointment(1, 1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteAppointment_ReturnsUnauthorized_WhenUserMismatch()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((false, "Unauthorized"));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.DeleteAppointment(1, 1);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public async Task DeleteAppointment_ReturnsNoContent_WhenSuccess()
        {
            var mockService = new Mock<IAppointmentService>();
            mockService.Setup(s => s.DeleteAppointmentAsync(1, 1))
                       .ReturnsAsync((true, null));

            var controller = new AppointmentsController(mockService.Object);
            var result = await controller.DeleteAppointment(1, 1);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
