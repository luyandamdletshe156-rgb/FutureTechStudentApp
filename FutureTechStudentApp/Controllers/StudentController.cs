using FutureTechStudentApp.Models;
using FutureTechStudentApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Security.Claims;

namespace FutureTechStudentApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<StudentController> _logger;
        private const int PageSize = 5;

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
            int currentPage = page < 1 ? 1 : page;

            string sql = "SELECT * FROM c WHERE 1=1";
            var queryDef = new QueryDefinition(sql);

            if (!string.IsNullOrEmpty(searchString))
            {
                string search = searchString.ToLower();
                sql += " AND (CONTAINS(LOWER(c.firstName), @search) OR CONTAINS(LOWER(c.lastName), @search) OR CONTAINS(LOWER(c.id), @search))";
                queryDef = new QueryDefinition(sql).WithParameter("@search", search);
            }

            sql += " OFFSET @offset LIMIT @limit";
            queryDef = queryDef.WithParameter("@offset", (currentPage - 1) * PageSize)
                               .WithParameter("@limit", PageSize);

            var students = await _cosmosDbService.GetStudentsAsync(queryDef);
            var totalCount = await _cosmosDbService.GetCountAsync(searchString);

            foreach (var student in students)
            {
                if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                    student.ProfileImageUrl = _blobStorageService.GetSecureImageUrl(student.ProfileImageUrl);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.ActiveCount = await _cosmosDbService.GetActiveCountAsync();

            return View(students);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile? profilePicture)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 🚨 NEW: Professional Catchy ID Generation (FT-2026-####)
                    string generatedId;
                    bool exists;
                    do
                    {
                        int randomPart = new Random().Next(1000, 9999);
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
                    _logger.LogError(ex, "Registration Failure");
                    ModelState.AddModelError("", "File upload failed. Please ensure the image is a valid JPG/PNG under 2MB.");
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