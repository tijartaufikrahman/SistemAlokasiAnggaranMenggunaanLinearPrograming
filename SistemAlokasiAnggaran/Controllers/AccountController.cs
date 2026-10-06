using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace SistemAlokasiAnggaran.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Login()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
                if (role == "Admin") return RedirectToAction("Dashboard", "Admin");
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VerifyOtp(string? email = null)
        {
            ViewData["Email"] = email ?? "";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token = null, string? email = null, string? otp = null)
        {
            ViewData["Token"] = token ?? "";
            ViewData["Email"] = email ?? "";
            ViewData["Otp"] = otp ?? "";
            return View();
        }

        [HttpPost]
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Logout()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("jwtToken");
            Response.Cookies.Delete("userName");
            Response.Cookies.Delete("userRole");
            return RedirectToAction(nameof(Login));
        }
    }
}
