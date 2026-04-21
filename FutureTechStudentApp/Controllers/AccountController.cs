using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FutureTechStudentApp.Controllers
{
    public class AccountController : Controller
    {
        // 🚨 THE VIP LIST: This is how we verify if they are an admin!
        private readonly List<string> _allowedAdminEmails = new List<string>
        {
            "gumedethingo867@gmail.com" // <-- Your admin email
        };

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, skip the login page and go to Dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Student");
            }
            return View();
        }

        // ---------------------------------------------------
        // 1. TRIGGER GOOGLE LOGIN
        // ---------------------------------------------------
        [AllowAnonymous]
        [HttpPost]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("OAuthResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // ---------------------------------------------------
        // 2. THE VERIFICATION STEP
        // ---------------------------------------------------
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> OAuthResponse()
        {
            // Read the info Google just sent back to our app
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Authentication failed or was canceled.";
                return RedirectToAction("Login");
            }

            // Extract the user's Email and Name from Google
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "Admin User";

            // --- THE SECURITY CHECK ---
            if (string.IsNullOrEmpty(email) || !_allowedAdminEmails.Contains(email.ToLower()))
            {
                // Kick them out! 
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = $"Access Denied. The email '{email}' is not an authorized administrator.";
                return RedirectToAction("Login");
            }

            // --- SUCCESS! THEY ARE AN ADMIN ---
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Admin") // <--- Role-Based Access Marks!
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Send them to the Dashboard!
            return RedirectToAction("Index", "Student");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}