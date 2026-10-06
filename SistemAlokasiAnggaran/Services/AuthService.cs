using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SistemAlokasiAnggaran.Interfaces;
using SistemAlokasiAnggaran.Models.Entities;

namespace SistemAlokasiAnggaran.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepo,
            IEmailService emailService,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _userRepo = userRepo;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        public async Task<User?> LoginAsync(string email, string password, int? roleId = null)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null || !user.IsActive) return null;
            if (roleId.HasValue && roleId.Value > 0 && user.RoleId != roleId.Value) return null;

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Format password hash tidak valid untuk user {Email}", email);
                isPasswordValid = false;
            }

            if (!isPasswordValid) return null;
            return user;
        }

        public string GenerateJwtToken(User user, string roleName)
        {
            var rawKey = _config["Jwt:Key"] ?? "KunciRahasia";
            var keyBytes = Encoding.UTF8.GetBytes(rawKey);
            var safeKeyBytes = keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
            var securityKey = new SymmetricSecurityKey(safeKeyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.NamaLengkap),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "KunciRahasia",
                audience: _config["Jwt:Audience"] ?? "KunciRahasiaApp",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<(bool Success, string Message, string? Error)> ForgotPasswordOtpAsync(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                return (false, "Email tidak terdaftar di sistem.", null);
            }

            // Generate 6 digit OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            user.ResetPasswordToken = otp;
            user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(5);

            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpEmailAsync(user.Email, user.NamaLengkap, otp);
                return (true, "Kode OTP berhasil dikirim ke email Anda.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengirim email OTP ke {Email}", user.Email);
                return (false, "Gagal mengirim email OTP.", ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null || user.ResetPasswordToken != otp || user.ResetPasswordExpiry < DateTime.UtcNow)
            {
                return (false, "Kode OTP tidak valid atau telah kedaluwarsa.");
            }

            return (true, "Kode OTP valid.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordWithOtpAsync(string email, string otp, string newPassword)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null || user.ResetPasswordToken != otp || user.ResetPasswordExpiry < DateTime.UtcNow)
            {
                return (false, "Kode OTP tidak valid atau telah kedaluwarsa.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();

            return (true, "Password berhasil diubah. Silakan login dengan password baru.");
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _userRepo.GetByResetTokenAsync(token);
            if (user == null || user.ResetPasswordExpiry < DateTime.UtcNow) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ValidateResetTokenAsync(string token)
        {
            var user = await _userRepo.GetByResetTokenAsync(token);
            if (user == null || user.ResetPasswordExpiry < DateTime.UtcNow) return false;
            return true;
        }

        public async Task<bool> ForgotPasswordAsync(string email, string resetBaseUrl)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) return false;

            var token = Guid.NewGuid().ToString("N");
            user.ResetPasswordToken = token;
            user.ResetPasswordExpiry = DateTime.UtcNow.AddHours(1);

            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();

            var resetLink = $"{resetBaseUrl}?token={token}";

            try
            {
                await _emailService.SendResetPasswordEmailAsync(user.Email, user.NamaLengkap, resetLink);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengirim email reset password");
                user.ResetPasswordToken = null;
                user.ResetPasswordExpiry = null;
                await _userRepo.UpdateAsync(user);
                await _userRepo.SaveChangesAsync();
                return false;
            }
        }
    }
}
