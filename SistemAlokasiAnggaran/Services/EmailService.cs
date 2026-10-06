using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SistemAlokasiAnggaran.Interfaces;

namespace SistemAlokasiAnggaran.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private (string host, int port, string email, string pass, string fromName) GetSmtpConfig()
        {
            var host = _config["EmailSettings:Host"] ?? _config["Smtp:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["EmailSettings:Port"] ?? _config["Smtp:Port"] ?? "587");
            var email = _config["EmailSettings:Email"] ?? _config["Smtp:Username"] ?? "";
            var pass = _config["EmailSettings:Password"] ?? _config["Smtp:Password"] ?? "";
            var fromName = _config["Smtp:FromName"] ?? "Sistem Alokasi Anggaran";
            return (host, port, email, pass, fromName);
        }

        public async Task SendOtpEmailAsync(string toEmail, string toName, string otp)
        {
            try
            {
                var (host, port, senderEmail, senderPassword, fromName) = GetSmtpConfig();

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, senderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = $"{otp} adalah Kode Verifikasi OTP Reset Password Anda";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                    <!DOCTYPE html>
                    <html lang='id'>
                    <head>
                        <meta charset='UTF-8'>
                        <style>
                            body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f1f5f9; margin: 0; padding: 24px; }}
                            .container {{ max-width: 480px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); border: 1px solid #e2e8f0; }}
                            .header {{ background: linear-gradient(135deg, #2dd4bf 0%, #0d9488 100%); padding: 32px 24px; text-align: center; color: #ffffff; }}
                            .header h1 {{ margin: 0; font-size: 20px; font-weight: 700; }}
                            .header p {{ margin: 6px 0 0; font-size: 13px; opacity: 0.9; }}
                            .content {{ padding: 32px 28px; text-align: center; color: #334155; }}
                            .greeting {{ font-size: 14px; margin-bottom: 16px; text-align: left; }}
                            .otp-box {{ background: #f0fdfa; border: 2px dashed #0d9488; border-radius: 12px; padding: 18px 24px; font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #0d9488; margin: 20px 0; display: inline-block; font-family: monospace; }}
                            .notice {{ font-size: 12.5px; color: #64748b; line-height: 1.6; margin-top: 16px; }}
                            .footer {{ background: #f8fafc; padding: 16px; font-size: 11.5px; color: #94a3b8; text-align: center; border-top: 1px solid #e2e8f0; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Sistem Alokasi Anggaran</h1>
                                <p>Permintaan Verifikasi Reset Password</p>
                            </div>
                            <div class='content'>
                                <div class='greeting'>Halo <strong>{toName}</strong>,</div>
                                <p style='font-size: 13.5px; line-height: 1.6; margin-bottom: 8px;'>Berikut adalah kode OTP untuk memverifikasi akun Anda:</p>
                                <div class='otp-box'>{otp}</div>
                                <div class='notice'>
                                    Kode OTP ini berlaku selama <strong>5 menit</strong>.<br>
                                    Jangan berikan kode ini kepada siapa pun demi keamanan akun Anda.
                                </div>
                            </div>
                            <div class='footer'>
                                &copy; 2026 Sistem Alokasi Anggaran. Email dikirim secara otomatis.
                            </div>
                        </div>
                    </body>
                    </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengirim email OTP ke {Email}", toEmail);
                throw;
            }
        }

        public async Task SendResetPasswordEmailAsync(string toEmail, string toName, string resetLink)
        {
            try
            {
                var (host, port, senderEmail, senderPassword, fromName) = GetSmtpConfig();

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, senderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = "Reset Password - Sistem Alokasi Anggaran";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                    <!DOCTYPE html>
                    <html lang='id'>
                    <head>
                        <meta charset='UTF-8'>
                        <style>
                            body {{ font-family: 'Segoe UI', sans-serif; background: #eef7f5; margin: 0; padding: 20px; }}
                            .container {{ max-width: 520px; margin: 0 auto; background: #fff; border-radius: 16px; overflow: hidden; box-shadow: 0 8px 24px rgba(0,0,0,.08); }}
                            .header {{ background: linear-gradient(135deg, #2dd4bf, #0d9488); padding: 30px; text-align: center; }}
                            .header h1 {{ color: #fff; margin: 0; font-size: 22px; }}
                            .header p {{ color: rgba(255,255,255,.85); margin: 6px 0 0; font-size: 13px; }}
                            .body {{ padding: 32px; }}
                            .body p {{ color: #475569; line-height: 1.7; font-size: 14px; }}
                            .btn {{ display: inline-block; margin: 20px 0; padding: 14px 32px; background: linear-gradient(135deg, #2dd4bf, #0d9488); color: #fff !important; text-decoration: none; border-radius: 10px; font-weight: 600; font-size: 14px; }}
                            .footer {{ background: #f8fafc; padding: 16px 32px; text-align: center; color: #94a3b8; font-size: 12px; }}
                            .warning {{ background: #fef3c7; border: 1px solid #fde68a; border-radius: 8px; padding: 12px 16px; margin-top: 16px; font-size: 12.5px; color: #92400e; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Sistem Alokasi Anggaran</h1>
                                <p>Reset Password Akun Anda</p>
                            </div>
                            <div class='body'>
                                <p>Halo <strong>{toName}</strong>,</p>
                                <p>Kami menerima permintaan untuk mereset password akun Anda di <strong>Sistem Alokasi Anggaran</strong>. Klik tombol di bawah untuk membuat password baru:</p>
                                <div style='text-align:center'>
                                    <a href='{resetLink}' class='btn'>&#x1F511; Reset Password Sekarang</a>
                                </div>
                                <div class='warning'>
                                    &#x26A0;&#xFE0F; <strong>Perhatian:</strong> Link ini hanya berlaku selama <strong>1 jam</strong>. Jika Anda tidak meminta reset password, abaikan email ini.
                                </div>
                                <p style='margin-top:16px;'>Atau copy link berikut ke browser Anda:<br>
                                <small style='color:#0d9488;word-break:break-all;'>{resetLink}</small></p>
                            </div>
                            <div class='footer'>
                                &copy; 2026 Sistem Alokasi Anggaran &mdash; Email otomatis, jangan dibalas.
                            </div>
                        </div>
                    </body>
                    </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengirim email reset password ke {Email}", toEmail);
                throw;
            }
        }
    }
}
