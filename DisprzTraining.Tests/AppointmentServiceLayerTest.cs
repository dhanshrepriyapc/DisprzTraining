using DisprzTraining.Business;
using DisprzTraining.DataAccess;
using DisprzTraining.DTOs;
using DisprzTraining.Models;
using Moq;

namespace DisprzTraining.Tests
{
    public class AppointmentServiceTests
    {
        #region GetAppointmentsForUserAsync

        [Fact]
        public async Task GetAppointmentsForUserAsync_ReturnsAppointments()
        {
            // Arrange
            var mockRepo = new Mock<IAppointmentRepository>();
            var appointments = new List<Appointment>
            {
                new Appointment { Id = 1, Title = "Meeting", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1), UserId = 1 }
            };
            mockRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(appointments);

            var service = new AppointmentService(mockRepo.Object);

            // Act
            var result = await service.GetAppointmentsForUserAsync(1);

            // Assert
            Assert.Single(result);
            Assert.Equal("Meeting", result[0].Title);
        }

        [Fact]
        public async Task GetAppointmentsForUserAsync_ReturnsEmptyList_WhenNoAppointments()
        {
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(new List<Appointment>());

            var service = new AppointmentService(mockRepo.Object);

            var result = await service.GetAppointmentsForUserAsync(1);

            Assert.Empty(result);
        }

        #endregion

        #region GetAppointmentByIdAsync

        [Fact]
        public async Task GetAppointmentByIdAsync_ReturnsAppointment()
        {
            var mockRepo = new Mock<IAppointmentRepository>();
            var appointment = new Appointment { Id = 1, Title = "Call", UserId = 1 };
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);

            var service = new AppointmentService(mockRepo.Object);

            var result = await service.GetAppointmentByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Call", result.Title);
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_ReturnsNull_WhenNotFound()
        {
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Appointment?)null);

            var service = new AppointmentService(mockRepo.Object);

            var result = await service.GetAppointmentByIdAsync(1);

            Assert.Null(result);
        }

        #endregion

        #region CreateAppointmentAsync

        [Fact]
        public async Task CreateAppointmentAsync_ReturnsSuccess_WhenNoOverlap()
        {
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(new List<Appointment>());

            var service = new AppointmentService(mockRepo.Object);
            var dto = new AppointmentDto { Title = "New Meeting", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) };

            var (success, error, appointment) = await service.CreateAppointmentAsync(dto, 1);

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal("New Meeting", appointment.Title);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_ReturnsConflict_WhenOverlapExists()
        {
            var existing = new List<Appointment>
            {
                new Appointment { StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1), UserId = 1 }
            };

            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(existing);

            var service = new AppointmentService(mockRepo.Object);

            var dto = new AppointmentDto
            {
                Title = "Overlap Meeting",
                StartTime = DateTime.Now.AddMinutes(30),
                EndTime = DateTime.Now.AddHours(1).AddMinutes(30)
            };

            var (success, error, appointment) = await service.CreateAppointmentAsync(dto, 1);

            Assert.False(success);
            Assert.Equal("Appointment time overlaps with existing appointment", error);
            Assert.Null(appointment);
        }

        #endregion

        #region UpdateAppointmentAsync

        [Fact]
        public async Task UpdateAppointmentAsync_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Appointment?)null);

            var service = new AppointmentService(mockRepo.Object);
            var dto = new AppointmentDto { Title = "Update" };

            var (success, error) = await service.UpdateAppointmentAsync(1, dto, 1);

            Assert.False(success);
            Assert.Equal("Not found", error);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_ReturnsUnauthorized_WhenUserMismatch()
        {
            var appointment = new Appointment { Id = 1, UserId = 2 };
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);

            var service = new AppointmentService(mockRepo.Object);
            var dto = new AppointmentDto { Title = "Update" };

            var (success, error) = await service.UpdateAppointmentAsync(1, dto, 1);

            Assert.False(success);
            Assert.Equal("Unauthorized", error);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_ReturnsConflict_WhenOverlapExists()
        {
            var appointment = new Appointment { Id = 1, UserId = 1, StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) };
            var existingAppointments = new List<Appointment>
            {
                appointment,
                new Appointment { Id = 2, UserId = 1, StartTime = DateTime.Now.AddMinutes(30), EndTime = DateTime.Now.AddHours(1).AddMinutes(30) }
            };

            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);
            mockRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(existingAppointments);

            var service = new AppointmentService(mockRepo.Object);
            var dto = new AppointmentDto { Title = "Updated", StartTime = DateTime.Now.AddMinutes(45), EndTime = DateTime.Now.AddHours(1).AddMinutes(45) };

            var (success, error) = await service.UpdateAppointmentAsync(1, dto, 1);

            Assert.False(success);
            Assert.Equal("Appointment time overlaps with existing appointment", error);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_ReturnsSuccess_WhenValid()
        {
            var appointment = new Appointment { Id = 1, UserId = 1, StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) };
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);
            mockRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(new List<Appointment> { appointment });

            var service = new AppointmentService(mockRepo.Object);
            var dto = new AppointmentDto { Title = "Updated", StartTime = appointment.StartTime, EndTime = appointment.EndTime };

            var (success, error) = await service.UpdateAppointmentAsync(1, dto, 1);

            Assert.True(success);
            Assert.Null(error);
            mockRepo.Verify(r => r.UpdateAsync(appointment), Times.Once);
        }

        #endregion

        #region DeleteAppointmentAsync

        [Fact]
        public async Task DeleteAppointmentAsync_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Appointment?)null);

            var service = new AppointmentService(mockRepo.Object);

            var (success, error) = await service.DeleteAppointmentAsync(1, 1);

            Assert.False(success);
            Assert.Equal("Not found", error);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_ReturnsUnauthorized_WhenUserMismatch()
        {
            var appointment = new Appointment { Id = 1, UserId = 2 };
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);

            var service = new AppointmentService(mockRepo.Object);

            var (success, error) = await service.DeleteAppointmentAsync(1, 1);

            Assert.False(success);
            Assert.Equal("Unauthorized", error);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_ReturnsSuccess_WhenUserIsOwner()
        {
            var appointment = new Appointment { Id = 1, UserId = 1 };
            var mockRepo = new Mock<IAppointmentRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);

            var service = new AppointmentService(mockRepo.Object);

            var (success, error) = await service.DeleteAppointmentAsync(1, 1);

            Assert.True(success);
            Assert.Null(error);
            mockRepo.Verify(r => r.DeleteAsync(appointment), Times.Once);
        }

        #endregion
    }
}
