using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using FutureTechStudentApp.Controllers;
using FutureTechStudentApp.Models;
using FutureTechStudentApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace FutureTechStudentApp.Tests
{
    public class StudentControllerTests
    {
        // Mocks for dependencies
        private readonly Mock<ICosmosDbService> _mockCosmos;
        private readonly Mock<IBlobStorageService> _mockBlob;
        private readonly Mock<ILogger<StudentController>> _mockLogger;
        private readonly StudentController _controller;

        public StudentControllerTests()
        {
            // Set up mocks
            _mockCosmos = new Mock<ICosmosDbService>();
            _mockBlob = new Mock<IBlobStorageService>();
            _mockLogger = new Mock<ILogger<StudentController>>();

            _controller = new StudentController(_mockCosmos.Object, _mockBlob.Object, _mockLogger.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.Name, "Test Admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var httpContext = new DefaultHttpContext() { User = user };

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

         
            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
        }

        
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfStudents()
        {
            
            var students = new List<Student>
            {
                new Student { Id = "FT-001", FirstName = "Test", LastName = "One", Email = "one@test.com", MobileNumber = "111", EnrolmentStatus = "Active", ProfileImageUrl = "" },
                new Student { Id = "FT-002", FirstName = "Test", LastName = "Two", Email = "two@test.com", MobileNumber = "222", EnrolmentStatus = "Active", ProfileImageUrl = "" }
            };
            _mockCosmos.Setup(s => s.GetStudentsAsync(It.IsAny<QueryDefinition>())).ReturnsAsync(students);
            _mockCosmos.Setup(s => s.GetCountAsync(It.IsAny<string>())).ReturnsAsync(2);

            var result = await _controller.Index(null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<IEnumerable<Student>>(viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Create_Post_ValidStudent_UploadsImageAndAddsToDb()
        {
            
            var testStudent = new Student { Id = "temp", FirstName = "Themba", LastName = "Xulu", Email = "themba@futuretechcom", MobileNumber = "0821234567", EnrolmentStatus = "Active", ProfileImageUrl = "" };
            var mockFile = new Mock<IFormFile>();

          
            mockFile.Setup(f => f.FileName).Returns("test-image.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(1024); 

            _mockBlob.Setup(b => b.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>())).ReturnsAsync("filename.jpg");
            _mockCosmos.Setup(c => c.GetStudentAsync(It.IsAny<string>())).ReturnsAsync((Student)null);

            var result = await _controller.Create(testStudent, mockFile.Object);

            Assert.IsType<RedirectToActionResult>(result);
            _mockCosmos.Verify(c => c.AddStudentAsync(It.IsAny<Student>()), Times.Once);
            _mockBlob.Verify(b => b.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_StudentWithImage_DeletesFromBlobAndCosmos()
        {
            string studentId = "FT-2026-001";
            var studentWithImage = new Student { Id = studentId, FirstName = "Themba", LastName = "Xulu", Email = "themba@futuretech.com", MobileNumber = "00585584", ProfileImageUrl = "custom-photo.jpg", EnrolmentStatus = "Active" };
            _mockCosmos.Setup(c => c.GetStudentAsync(studentId)).ReturnsAsync(studentWithImage);

            var result = await _controller.DeleteConfirmed(studentId);

            Assert.IsType<RedirectToActionResult>(result);
            _mockCosmos.Verify(c => c.DeleteStudentAsync(studentId), Times.Once);
            _mockBlob.Verify(b => b.DeleteImageAsync("custom-photo.jpg"), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_StudentWithDefaultAvatar_OnlyDeletesFromCosmos()
        {
            string studentId = "FT-2026-002";
            var studentWithDefaultAvatar = new Student { Id = studentId, FirstName = "Minehle", LastName = "Dlamini", Email = "Minenhle@futuretech", MobileNumber = "07585858", ProfileImageUrl = "default-avatar.png", EnrolmentStatus = "Active" };
            _mockCosmos.Setup(c => c.GetStudentAsync(studentId)).ReturnsAsync(studentWithDefaultAvatar);

            var result = await _controller.DeleteConfirmed(studentId);

            Assert.IsType<RedirectToActionResult>(result);
            _mockCosmos.Verify(c => c.DeleteStudentAsync(studentId), Times.Once);
            _mockBlob.Verify(b => b.DeleteImageAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Get_NonExistentId_ReturnsNotFound()
        {
            string nonExistentId = "FT-2026-999";
            _mockCosmos.Setup(c => c.GetStudentAsync(nonExistentId)).ReturnsAsync((Student)null);

            var result = await _controller.Delete(nonExistentId);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}