using SistemAlokasiAnggaran.Models.Entities;

namespace SistemAlokasiAnggaran.Interfaces
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string email, string password, int? roleId = null);
        string GenerateJwtToken(User user, string roleName);
        Task<(bool Success, string Message, string? Error)> ForgotPasswordOtpAsync(string email);
        Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp);
        Task<(bool Success, string Message)> ResetPasswordWithOtpAsync(string email, string otp, string newPassword);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<bool> ValidateResetTokenAsync(string token);
        Task<bool> ForgotPasswordAsync(string email, string resetBaseUrl);
    }
}
