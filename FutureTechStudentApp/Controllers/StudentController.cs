using FutureTechStudentApp.Models;
using FutureTechStudentApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Security.Claims;
using System.IO; 
using System.Linq; 
namespace FutureTechStudentApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentController : Controller
    {
        /*These are Dependency Injection (DI) fields, ASP.NET Core automatically
        provides instances of your database service (CosmosDB), file storage service (Azure Blob),
        and logging service. PageSize dictates that only 5 students will be shown per page on the Index list.*/

        private readonly ICosmosDbService _cosmosDbService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<StudentController> _logger;
        private const int PageSize = 5;

        // The constructor initializes the controller with the injected services. This allows the controller to interact with the database, file storage, and logging without needing to know the details of how those services are implemented.
        public StudentController(ICosmosDbService cosmosDbService, IBlobStorageService blobStorageService, ILogger<StudentController> logger)
        {
            _cosmosDbService = cosmosDbService;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

   
        private string CurrentAdminInfo =>
            $"{User.Identity?.Name ?? "Unknown Admin"} ({User.FindFirstValue(ClaimTypes.Email) ?? "N/A"})";

    
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
        
            ViewData["CurrentFilter"] = searchString;

            int currentPage = page;
            if (!string.IsNullOrEmpty(searchString))
            {
      
                if (TempData["LastSearch"]?.ToString() != searchString)
                {
                    currentPage = 1;
                }
                TempData["LastSearch"] = searchString;
                TempData.Keep("LastSearch"); 
            }
            else
            {
                TempData["LastSearch"] = null;
            }

            currentPage = currentPage < 1 ? 1 : currentPage;

            // 3. Build the SQL string
            string sql = "SELECT * FROM c WHERE 1=1";
            if (!string.IsNullOrEmpty(searchString))
            {
                sql += " AND (CONTAINS(LOWER(c.firstName), @search) OR CONTAINS(LOWER(c.lastName), @search) OR CONTAINS(LOWER(c.id), @search))";
            }
            sql += " OFFSET @offset LIMIT @limit";

            var queryDef = new QueryDefinition(sql)
                .WithParameter("@offset", (currentPage - 1) * PageSize)
                .WithParameter("@limit", PageSize);

            if (!string.IsNullOrEmpty(searchString))
            {
                queryDef = queryDef.WithParameter("@search", searchString.ToLower());
            }

           
            var students = await _cosmosDbService.GetStudentsAsync(queryDef);
            var totalCount = await _cosmosDbService.GetCountAsync(searchString);

       
            foreach (var student in students)
            {
                if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                    student.ProfileImageUrl = _blobStorageService.GetSecureImageUrl(student.ProfileImageUrl);
            }

       
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / PageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.ActiveCount = await _cosmosDbService.GetActiveCountAsync();

            return View(students);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile? profilePicture)
        {
          
            if (profilePicture != null)
            {
           
                const long maxFileSize = 2 * 1024 * 1024; 
                if (profilePicture.Length > maxFileSize)
                {
                    ModelState.AddModelError("", "The profile picture must be less than 2MB.");
                }

               
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();

      
                var allowedMimeTypes = new[] { "image/jpeg", "image/png" };

                if (!allowedExtensions.Contains(extension) || !allowedMimeTypes.Contains(profilePicture.ContentType))
                {
                    ModelState.AddModelError("", "Invalid file type. Only JPG and PNG images are allowed.");
                }
            }

        
            if (ModelState.IsValid)
            {
                try
                {
                    string generatedId;
                    bool exists;
                    do
                    {
                        int randomPart = Random.Shared.Next(1000, 9999);
                        generatedId = $"FT-{DateTime.Now.Year}-{randomPart}";
                        var check = await _cosmosDbService.GetStudentAsync(generatedId);
                        exists = (check != null);
                    } while (exists);

                    student.Id = generatedId;

                    string fileName = profilePicture != null
                        ? await _blobStorageService.UploadImageAsync(profilePicture, student.Id)
                        : "default-avatar.png";

                    student.ProfileImageUrl = fileName;
                    await _cosmosDbService.AddStudentAsync(student);

                    _logger.LogInformation("Audit: Student {Id} registered by {Admin}", student.Id, CurrentAdminInfo);
                    TempData["SuccessMessage"] = $"Student registered successfully with ID: {student.Id}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Registration Failure for Student ID: {Id}", student.Id);
                    ModelState.AddModelError("", "An error occurred while saving the student to the database.");
                }
            }

   
            return View(student);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null) return NotFound();

            student.ProfileImageUrl = _blobStorageService.GetSecureImageUrl(student.ProfileImageUrl!);
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Student student)
        {
            if (id != student.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingStudent = await _cosmosDbService.GetStudentAsync(id);
                if (existingStudent == null) return NotFound();

                existingStudent.FirstName = student.FirstName;
                existingStudent.LastName = student.LastName;
                existingStudent.Email = student.Email;
                existingStudent.MobileNumber = student.MobileNumber;
                existingStudent.EnrolmentStatus = student.EnrolmentStatus;
                // Note: Image update logic is intentionally absent here per original design

                await _cosmosDbService.UpdateStudentAsync(existingStudent.Id, existingStudent);
                TempData["SuccessMessage"] = "Student details updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null) return NotFound();

            student.ProfileImageUrl = _blobStorageService.GetSecureImageUrl(student.ProfileImageUrl!);
            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student != null)
            {
                if (!string.IsNullOrEmpty(student.ProfileImageUrl) && student.ProfileImageUrl != "default-avatar.png")
                {
                    await _blobStorageService.DeleteImageAsync(student.ProfileImageUrl);
                }
                await _cosmosDbService.DeleteStudentAsync(id);
                _logger.LogWarning("Audit: Student {Id} PERMANENTLY REMOVED by {Admin}", id, CurrentAdminInfo);
                TempData["SuccessMessage"] = "Student record permanently deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(string id)
        {
            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student != null)
            {
                student.EnrolmentStatus = "Inactive";
                await _cosmosDbService.UpdateStudentAsync(id, student);
                _logger.LogInformation("Audit: Student {Id} deactivated by {Admin}", id, CurrentAdminInfo);
                TempData["SuccessMessage"] = "Student marked as Inactive.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}