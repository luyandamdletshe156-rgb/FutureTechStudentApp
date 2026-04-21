using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using FutureTechStudentApp.Controllers;
using FutureTechStudentApp.Models;
using FutureTechStudentApp.Services;
using Microsoft.Extensions.Logging;

namespace FutureTechStudentApp.Tests
{
    public class StudentControllerTests
    {
        private readonly Mock<ICosmosDbService> _mockCosmos;
        private readonly Mock<IBlobStorageService> _mockBlob;
        private readonly Mock<ILogger<StudentController>> _mockLogger;
        private readonly StudentController _controller;

        public StudentControllerTests()
        {
            _mockCosmos = new Mock<ICosmosDbService>();
            _mockBlob = new Mock<IBlobStorageService>();
            _mockLogger = new Mock<ILogger<StudentController>>();

            _controller = new StudentController(_mockCosmos.Object, _mockBlob.Object, _mockLogger.Object);

            // Simulating an Admin User
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.Name, "Test Admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task Create_Post_ValidData_CallsCloudServicesAndRedirects()
        {
            var testStudent = new Student { FirstName = "John", LastName = "Doe", Email = "john@doe.com", MobileNumber = "0123456789" };
            var mockFile = new Mock<IFormFile>();

            _mockBlob.Setup(b => b.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>())).ReturnsAsync("filename.jpg");

            var result = await _controller.Create(testStudent, mockFile.Object);

            Assert.IsType<RedirectToActionResult>(result);
            _mockCosmos.Verify(c => c.AddStudentAsync(It.IsAny<Student>()), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_ValidId_RemovesFromCosmosAndRedirects()
        {
            // Arrange
            string studentId = "test-123";
            var existingStudent = new Student { Id = studentId, FirstName = "John", LastName = "Doe", Email = "a@a.com", MobileNumber = "123" };

            // Fix: Mock the return value so the controller finds the student
            _mockCosmos.Setup(c => c.GetStudentAsync(studentId)).ReturnsAsync(existingStudent);

            // Act
            var result = await _controller.DeleteConfirmed(studentId);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            _mockCosmos.Verify(c => c.DeleteStudentAsync(studentId), Times.Once);
        }
    }
}