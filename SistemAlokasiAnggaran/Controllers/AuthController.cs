using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemAlokasiAnggaran.Interfaces;
using SistemAlokasiAnggaran.Models.DTOs;

namespace SistemAlokasiAnggaran.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [HttpPost("/api/users/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { success = false, message = "Email dan password wajib diisi." });

            var user = await _authService.LoginAsync(model.Email, model.Password, model.RoleId);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Email atau password salah. Silakan coba lagi." });
            }

            var roleName = user.Role?.NamaRole ?? "Guest";
            var token = _authService.GenerateJwtToken(user, roleName);

            // Set cookie auth agar kompatibel dengan MVC Razor Views
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.NamaLengkap),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("RoleId", user.RoleId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            _logger.LogInformation("User {Email} berhasil login. JWT Token berhasil digenerate.", model.Email);

            return Ok(new
            {
                success = true,
                message = "Login berhasil.",
                token = token,
                user = new UserAuthDto
                {
                    Id = user.Id,
                    NamaLengkap = user.NamaLengkap,
                    Email = user.Email,
                    RoleId = user.RoleId,
                    RoleName = roleName
                }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains('@'))
                return BadRequest(new { success = false, message = "Format email tidak valid." });

            var result = await _authService.ForgotPasswordOtpAsync(model.Email.Trim());
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message, error = result.Error });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Otp))
                return BadRequest(new { success = false, message = "Email dan kode OTP wajib diisi." });

            var result = await _authService.VerifyOtpAsync(model.Email.Trim(), model.Otp.Trim());
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
                return BadRequest(new { success = false, message = "Password minimal 6 karakter." });

            if (!string.IsNullOrEmpty(model.ConfirmPassword) && model.NewPassword != model.ConfirmPassword)
                return BadRequest(new { success = false, message = "Password dan konfirmasi password tidak cocok." });

            // Jika menggunakan OTP (email + otp)
            if (!string.IsNullOrWhiteSpace(model.Email) && !string.IsNullOrWhiteSpace(model.Otp))
            {
                var otpResult = await _authService.ResetPasswordWithOtpAsync(model.Email.Trim(), model.Otp.Trim(), model.NewPassword);
                if (!otpResult.Success)
                {
                    return BadRequest(new { success = false, message = otpResult.Message });
                }

                return Ok(new { success = true, message = otpResult.Message });
            }

            // Jika menggunakan Token biasa (link)
            if (!string.IsNullOrWhiteSpace(model.Token))
            {
                var tokenResult = await _authService.ResetPasswordAsync(model.Token, model.NewPassword);
                if (!tokenResult)
                {
                    return BadRequest(new { success = false, message = "Link reset password tidak valid atau telah kedaluwarsa." });
                }

                return Ok(new { success = true, message = "Password berhasil diubah! Silakan login kembali." });
            }

            return BadRequest(new { success = false, message = "Data verifikasi reset password tidak lengkap." });
        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { success = false, valid = false, message = "Token tidak boleh kosong." });

            var isValid = await _authService.ValidateResetTokenAsync(token);
            if (!isValid)
            {
                return Ok(new
                {
                    success = false,
                    valid = false,
                    message = "Link reset password tidak valid atau telah kedaluwarsa."
                });
            }

            return Ok(new { success = true, valid = true, message = "Token valid." });
        }
    }
}
