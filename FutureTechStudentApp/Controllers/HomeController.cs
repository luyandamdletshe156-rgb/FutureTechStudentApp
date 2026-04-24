using FutureTechStudentApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FutureTechStudentApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

     
        public IActionResult Index()
        {
            _logger.LogInformation("FutureTech Portal: Public landing page accessed at {Time}", DateTime.UtcNow);
            return View();
        }

    
        public IActionResult Privacy()
        {
            _logger.LogInformation("Security Protocol: Data Governance policy accessed.");
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

     
            _logger.LogError("Critical System Exception. Correlation ID: {RequestId}", requestId);

            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}