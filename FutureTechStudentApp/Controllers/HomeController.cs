using FutureTechStudentApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FutureTechStudentApp.Controllers
{
    /// <summary>
    /// HomeController manages the public entry points for the FutureTech Academy Portal.
    /// This controller remains accessible to all users (Public Access).
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // --- 1. CORPORATE LANDING PORTAL ---
        // GET: /Home/Index
        // Displays the institutional hero page and high-level system features.
        public IActionResult Index()
        {
            _logger.LogInformation("FutureTech Portal: Public landing page accessed at {Time}", DateTime.UtcNow);
            return View();
        }

        // --- 2. DATA GOVERNANCE & PRIVACY POLICY ---
        // GET: /Home/Privacy
        // Displays the standalone POPIA and Azure Security compliance document.
        public IActionResult Privacy()
        {
            _logger.LogInformation("Security Protocol: Data Governance policy accessed.");
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        // --- 3. SYSTEM DIAGNOSTICS & ERROR HANDLING ---
        // This action captures all unhandled exceptions and logs a unique Request ID.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Enterprise Logging: Captured for Azure Application Insights
            _logger.LogError("Critical System Exception. Correlation ID: {RequestId}", requestId);

            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}