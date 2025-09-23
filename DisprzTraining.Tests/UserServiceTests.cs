using DisprzTraining.Business;
using DisprzTraining.DataAccess;
using DisprzTraining.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DisprzTraining.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _mockConfig = new Mock<IConfiguration>();
            
            // Setup default configuration values
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("ThisIsAReallyLongSuperSecretKey123!");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestApp");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestApp");
            
            _service = new UserService(_mockRepository.Object, _mockConfig.Object);
        }

        #region AuthenticateAsync Tests

        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsUser()
        {
            // Arrange
            var username = "testuser";
            var password = "testpassword";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new User
            {
                Id = 1,
                Username = username,
                PasswordHash = hashedPassword,
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync(user);

            // Act
            var result = await _service.AuthenticateAsync(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task AuthenticateAsync_UserNotFound_ReturnsNull()
        {
            // Arrange
            var username = "nonexistentuser";
            var password = "testpassword";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);

            // Act
            var result = await _service.AuthenticateAsync(username, password);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidPassword_ReturnsNull()
        {
            // Arrange
            var username = "testuser";
            var correctPassword = "correctpassword";
            var wrongPassword = "wrongpassword";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(correctPassword);
            
            var user = new User
            {
                Id = 1,
                Username = username,
                PasswordHash = hashedPassword,
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync(user);

            // Act
            var result = await _service.AuthenticateAsync(username, wrongPassword);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_EmptyUsername_ReturnsNull()
        {
            // Arrange
            var username = "";
            var password = "testpassword";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);

            // Act
            var result = await _service.AuthenticateAsync(username, password);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_EmptyPassword_ReturnsNull()
        {
            // Arrange
            var username = "testuser";
            var password = "";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("actualpassword");
            
            var user = new User
            {
                Id = 1,
                Username = username,
                PasswordHash = hashedPassword,
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync(user);

            // Act
            var result = await _service.AuthenticateAsync(username, password);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GenerateJwtToken Tests

        [Fact]
        public void GenerateJwtToken_ValidUser_ReturnsValidToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            // Act
            var token = _service.GenerateJwtToken(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Verify token can be parsed
            var tokenHandler = new JwtSecurityTokenHandler();
            Assert.True(tokenHandler.CanReadToken(token));
            
            var jwtToken = tokenHandler.ReadJwtToken(token);
            Assert.Equal("testuser", jwtToken.Subject);
            Assert.Contains(jwtToken.Claims, c => c.Type == "id" && c.Value == "1");
            Assert.Contains(jwtToken.Claims, c => c.Type == "timeZoneId" && c.Value == "UTC");
        }

        [Fact]
        public void GenerateJwtToken_UserWithDifferentTimeZone_IncludesCorrectTimeZone()
        {
            // Arrange
            var user = new User
            {
                Id = 2,
                Username = "timezoneuser",
                FirstName = "TimeZone",
                LastName = "User",
                TimeZoneId = "America/New_York"
            };

            // Act
            var token = _service.GenerateJwtToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            Assert.Contains(jwtToken.Claims, c => c.Type == "timeZoneId" && c.Value == "America/New_York");
        }

        [Fact]
        public void GenerateJwtToken_ShortJwtKey_PadsKeyCorrectly()
        {
            // Arrange
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("short"); // Less than 32 characters
            var service = new UserService(_mockRepository.Object, _mockConfig.Object);
            
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            // Act & Assert - Should not throw exception
            var token = service.GenerateJwtToken(user);
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateJwtToken_NoJwtKeyInConfig_UsesDefaultKey()
        {
            // Arrange
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns((string?)null);
            var service = new UserService(_mockRepository.Object, _mockConfig.Object);
            
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            // Act & Assert - Should not throw exception
            var token = service.GenerateJwtToken(user);
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateJwtToken_NoIssuerInConfig_UsesDefaultIssuer()
        {
            // Arrange
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns((string?)null);
            var service = new UserService(_mockRepository.Object, _mockConfig.Object);
            
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            // Act
            var token = service.GenerateJwtToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            Assert.Equal("MyApp", jwtToken.Issuer);
        }

        [Fact]
        public void GenerateJwtToken_NoAudienceInConfig_UsesDefaultAudience()
        {
            // Arrange
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns((string?)null);
            var service = new UserService(_mockRepository.Object, _mockConfig.Object);
            
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            // Act
            var token = service.GenerateJwtToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            Assert.Contains("MyApp", jwtToken.Audiences);
        }

        [Fact]
        public void GenerateJwtToken_ValidUser_TokenExpiresIn8Hours()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            var beforeGeneration = DateTime.UtcNow;

            // Act
            var token = _service.GenerateJwtToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var expectedExpiry = beforeGeneration.AddHours(8);
            var actualExpiry = jwtToken.ValidTo;
            
            // Allow for small time differences (within 1 minute)
            Assert.True(Math.Abs((expectedExpiry - actualExpiry).TotalMinutes) < 1);
        }

        [Fact]
        public void GenerateJwtToken_ValidUser_ContainsJtiClaim()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            // Act
            var token = _service.GenerateJwtToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
            Assert.NotNull(jtiClaim);
            Assert.True(Guid.TryParse(jtiClaim.Value, out _)); // Should be a valid GUID
        }

        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_ValidData_CreatesAndReturnsUser()
        {
            // Arrange
            var username = "newuser";
            var firstName = "New";
            var lastName = "User";
            var password = "password123";
            var timeZoneId = "UTC";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null); // User doesn't exist
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            var result = await _service.RegisterAsync(username, firstName, lastName, password, timeZoneId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
            Assert.Equal(firstName, result.FirstName);
            Assert.Equal(lastName, result.LastName);
            Assert.Equal(timeZoneId, result.TimeZoneId);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, result.PasswordHash));
            
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ExistingUsername_ThrowsException()
        {
            // Arrange
            var username = "existinguser";
            var firstName = "Existing";
            var lastName = "User";
            var password = "password123";
            
            var existingUser = new User
            {
                Id = 1,
                Username = username,
                FirstName = "Existing",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.RegisterAsync(username, firstName, lastName, password));
            
            Assert.Equal("Username already exists", exception.Message);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_NoTimeZoneProvided_UsesLocalTimeZone()
        {
            // Arrange
            var username = "newuser";
            var firstName = "New";
            var lastName = "User";
            var password = "password123";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            var result = await _service.RegisterAsync(username, firstName, lastName, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TimeZoneInfo.Local.Id, result.TimeZoneId);
        }

        [Fact]
        public async Task RegisterAsync_NullTimeZone_UsesLocalTimeZone()
        {
            // Arrange
            var username = "newuser";
            var firstName = "New";
            var lastName = "User";
            var password = "password123";
            string? timeZoneId = null;

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            var result = await _service.RegisterAsync(username, firstName, lastName, password, timeZoneId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TimeZoneInfo.Local.Id, result.TimeZoneId);
        }

        [Fact]
        public async Task RegisterAsync_ValidData_HashesPassword()
        {
            // Arrange
            var username = "newuser";
            var firstName = "New";
            var lastName = "User";
            var password = "plainTextPassword";
            var timeZoneId = "UTC";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            var result = await _service.RegisterAsync(username, firstName, lastName, password, timeZoneId);

            // Assert
            Assert.NotEqual(password, result.PasswordHash); // Password should be hashed
            Assert.True(BCrypt.Net.BCrypt.Verify(password, result.PasswordHash)); // But should verify correctly
        }

        [Fact]
        public async Task RegisterAsync_EmptyUsername_StillProcesses()
        {
            // Arrange
            var username = "";
            var firstName = "New";
            var lastName = "User";
            var password = "password123";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            var result = await _service.RegisterAsync(username, firstName, lastName, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
        }

        #endregion

        #region UpdateTimeZoneAsync Tests

        [Fact]
        public async Task UpdateTimeZoneAsync_ValidUserAndTimeZone_UpdatesSuccessfully()
        {
            // Arrange
            var userId = 1;
            var newTimeZoneId = "America/New_York";
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC",
                FirstName = "Test",
                LastName = "User"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(userId))
                          .ReturnsAsync(user);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateTimeZoneAsync(userId, newTimeZoneId);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.Equal(newTimeZoneId, user.TimeZoneId);
            _mockRepository.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateTimeZoneAsync_UserNotFound_ReturnsError()
        {
            // Arrange
            var userId = 999;
            var newTimeZoneId = "America/New_York";

            _mockRepository.Setup(r => r.GetByIdAsync(userId))
                          .ReturnsAsync((User?)null);

            // Act
            var result = await _service.UpdateTimeZoneAsync(userId, newTimeZoneId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Error);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTimeZoneAsync_InvalidTimeZone_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var invalidTimeZoneId = "Invalid/TimeZone";
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC",
                FirstName = "Test",
                LastName = "User"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(userId))
                          .ReturnsAsync(user);

            // Act
            var result = await _service.UpdateTimeZoneAsync(userId, invalidTimeZoneId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid time zone ID", result.Error);
            Assert.Equal("UTC", user.TimeZoneId); // Should remain unchanged
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTimeZoneAsync_RepositoryThrowsException_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var newTimeZoneId = "America/New_York";
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC",
                FirstName = "Test",
                LastName = "User"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(userId))
                          .ReturnsAsync(user);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                          .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.UpdateTimeZoneAsync(userId, newTimeZoneId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Database error", result.Error);
        }

        [Fact]
        public async Task UpdateTimeZoneAsync_ValidTimeZoneIds_AllWork()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC",
                FirstName = "Test",
                LastName = "User"
            };

            var validTimeZones = new[]
            {
                "UTC",
                "America/New_York",
                "Europe/London",
                "Asia/Tokyo",
                "Australia/Sydney"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(userId))
                          .ReturnsAsync(user);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act & Assert
            foreach (var timeZoneId in validTimeZones)
            {
                var result = await _service.UpdateTimeZoneAsync(userId, timeZoneId);
                Assert.True(result.Success, $"Failed for timezone: {timeZoneId}");
                Assert.Null(result.Error);
                Assert.Equal(timeZoneId, user.TimeZoneId);
            }
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public async Task RegisterAsync_RepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var username = "newuser";
            var firstName = "New";
            var lastName = "User";
            var password = "password123";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                          .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.RegisterAsync(username, firstName, lastName, password));
            
            Assert.Equal("Database connection failed", exception.Message);
        }

        [Fact]
        public async Task AuthenticateAsync_RepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var username = "testuser";
            var password = "testpassword";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.AuthenticateAsync(username, password));
            
            Assert.Equal("Database connection failed", exception.Message);
        }

        [Fact]
        public void GenerateJwtToken_MultipleTokensForSameUser_HaveDifferentJti()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                TimeZoneId = "UTC"
            };

            // Act
            var token1 = _service.GenerateJwtToken(user);
            var token2 = _service.GenerateJwtToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken1 = tokenHandler.ReadJwtToken(token1);
            var jwtToken2 = tokenHandler.ReadJwtToken(token2);
            
            var jti1 = jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var jti2 = jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            
            Assert.NotEqual(jti1, jti2);
        }

        [Fact]
        public async Task UpdateTimeZoneAsync_EmptyTimeZoneId_ReturnsError()
        {
            // Arrange
            var userId = 1;
            var emptyTimeZoneId = "";
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TimeZoneId = "UTC",
                FirstName = "Test",
                LastName = "User"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(userId))
                          .ReturnsAsync(user);

            // Act
            var result = await _service.UpdateTimeZoneAsync(userId, emptyTimeZoneId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid time zone ID", result.Error);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task AuthenticateAsync_CallsRepositoryWithCorrectUsername()
        {
            // Arrange
            var username = "specificuser";
            var password = "password";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);

            // Act
            await _service.AuthenticateAsync(username, password);

            // Assert
            _mockRepository.Verify(r => r.GetByUsernameAsync("specificuser"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_CallsGetByUsernameBeforeAdd()
        {
            // Arrange
            var username = "newuser";
            var firstName = "New";
            var lastName = "User";
            var password = "password123";

            _mockRepository.Setup(r => r.GetByUsernameAsync(username))
                          .ReturnsAsync((User?)null);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            await _service.RegisterAsync(username, firstName, lastName, password);

            // Assert
            _mockRepository.Verify(r => r.GetByUsernameAsync(username), Times.Once);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task UpdateTimeZoneAsync_CallsGetByIdBeforeUpdate()
        {
            // Arrange
            var userId = 1;
            var newTimeZoneId = "America/New_York";
            var user = new User { Id = userId, Username = "test", TimeZoneId = "UTC" };

            _mockRepository.Setup(r => r.GetByIdAsync(userId))
                          .ReturnsAsync(user);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                          .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateTimeZoneAsync(userId, newTimeZoneId);

            // Assert
            _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        #endregion
    }
}
