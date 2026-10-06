namespace SistemAlokasiAnggaran.Interfaces
{
    public interface IEmailService
    {
        Task SendResetPasswordEmailAsync(string toEmail, string toName, string resetLink);
        Task SendOtpEmailAsync(string toEmail, string toName, string otp);
    }
}
