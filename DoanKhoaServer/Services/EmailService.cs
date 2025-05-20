using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DoanKhoaServer.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly ILogger<EmailService> _logger;
        private readonly bool _emailEnabled;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger = null)
        {
            _logger = logger;

            // Check if EmailSettings section exists
            if (configuration.GetSection("EmailSettings").Exists())
            {
                _smtpServer = configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                _smtpPort = !string.IsNullOrEmpty(configuration["EmailSettings:SmtpPort"])
                    ? int.Parse(configuration["EmailSettings:SmtpPort"])
                    : 587;
                _smtpUsername = configuration["EmailSettings:Username"] ?? "";
                _smtpPassword = configuration["EmailSettings:Password"] ?? "";
                _senderEmail = configuration["EmailSettings:SenderEmail"] ?? "no-reply@example.com";
                _senderName = configuration["EmailSettings:SenderName"] ?? "Đoàn Khoa MMT&TT";
                _emailEnabled = true;
            }
            else
            {
                // Use defaults if section is missing
                _logger?.LogWarning("EmailSettings section not found in configuration. Using default values.");
                _smtpServer = "smtp.gmail.com";
                _smtpPort = 587;
                _smtpUsername = "";
                _smtpPassword = "";
                _senderEmail = "no-reply@example.com";
                _senderName = "Đoàn Khoa MMT&TT";
                _emailEnabled = false;
            }
        }

        public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body, bool isHtml = true)
        {
            // If email is not configured properly, log warning and return "success" to not block registration flow
            if (!_emailEnabled || string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger?.LogWarning($"Email sending is disabled or not configured. Would have sent email to: {recipientEmail}");
                return true;
            }

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, _senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                message.To.Add(new MailAddress(recipientEmail));

                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

                    await client.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string name, string verificationCode)
        {
            string subject = "Xác thực tài khoản Đoàn Khoa MMT&TT";

            // Create HTML email body with verification code
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                        <h2 style='color: #042354;'>Xác thực tài khoản</h2>
                        <p>Xin chào <strong>{name}</strong>,</p>
                        <p>Cảm ơn bạn đã đăng ký tài khoản tại hệ thống Đoàn Khoa Mạng máy tính và Truyền thông.</p>
                        <p>Để hoàn tất đăng ký, vui lòng nhập mã xác thực sau:</p>
                        <div style='background-color: #f5f5f5; padding: 15px; text-align: center; border-radius: 5px;'>
                            <h3 style='font-size: 24px; letter-spacing: 5px; margin: 0;'>{verificationCode}</h3>
                        </div>
                        <p>Mã xác thực có hiệu lực trong vòng 15 phút.</p>
                        <p>Nếu bạn không yêu cầu đăng ký tài khoản, vui lòng bỏ qua email này.</p>
                        <hr style='border: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #666;'>Đây là email tự động, vui lòng không trả lời.</p>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(email, subject, body);
        }
    }
}