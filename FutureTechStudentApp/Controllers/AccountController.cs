using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FutureTechStudentApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly List<string> _allowedAdminEmails = new List<string>
        {
            "gumedethingo867@gmail.com",
            "luyandamdletshe156@gmail.com"
        };

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Student");
            }
            return View();
        }

     
        [AllowAnonymous]
        [HttpPost]
        public IActionResult ExternalLogin(string provider)
        {
        
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("OAuthResponse") };
            return Challenge(properties, provider);
        }

      
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> OAuthResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Authentication failed or was canceled.";
                return RedirectToAction("Login");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "Admin User";

            if (string.IsNullOrEmpty(email) || !_allowedAdminEmails.Contains(email.ToLower()))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = $"Access Denied. The email '{email}' is not an authorized administrator.";
                return RedirectToAction("Login");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

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