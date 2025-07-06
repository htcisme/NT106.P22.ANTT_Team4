using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderPassword;
        private readonly string _senderName;

        public EmailService()
        {
            // Cấu hình SMTP - có thể lấy từ appsettings.json hoặc config
            _smtpServer = "smtp.gmail.com"; // Hoặc smtp server khác
            _smtpPort = 587;
            _senderEmail = "your-app-email@gmail.com"; // Email gửi từ app
            _senderPassword = "your-app-password"; // App password
            _senderName = "Task Management System";
        }

        public async Task<bool> SendTaskReminderEmailAsync(string recipientEmail, string recipientName, TaskItem task, int daysRemaining)
        {
            try
            {
                var subject = $"[NHẮC NHỞ] Công việc \"{task.Title}\" sắp đến hạn";
                var body = GenerateReminderEmailBody(recipientName, task, daysRemaining);

                return await SendEmailAsync(recipientEmail, recipientName, subject, body);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gửi email nhắc nhở: {ex.Message}");
                return false;
            }
        }

        // THÊM: Method gửi reminder manual từ Admin
        public async Task<bool> SendManualReminderEmailAsync(string recipientEmail, string recipientName, TaskItem task)
        {
            try
            {
                var subject = $"[NHẮC NHỞ MANUAL] Công việc \"{task.Title}\" cần được hoàn thành";
                var body = GenerateManualReminderEmailBody(recipientName, task);

                return await SendEmailAsync(recipientEmail, recipientName, subject, body);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gửi manual reminder: {ex.Message}");
                return false;
            }
        }

        private string GenerateReminderEmailBody(string recipientName, TaskItem task, int daysRemaining)
        {
            var urgencyText = daysRemaining == 1 ? "KHẨN CẤP - Còn 1 ngày" : "Còn 2 ngày";
            var urgencyColor = daysRemaining == 1 ? "#dc3545" : "#fd7e14";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Nhắc nhở công việc</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px 10px 0 0;'>
            <h1 style='margin: 0; font-size: 24px;'>📋 Nhắc Nhở Công Việc</h1>
        </div>
        
        <div style='background: #f8f9fa; padding: 20px; border: 1px solid #e9ecef;'>
            <p style='margin: 0 0 15px 0; font-size: 16px;'>Xin chào <strong>{recipientName}</strong>,</p>
            
            <div style='background: {urgencyColor}; color: white; padding: 10px 15px; border-radius: 5px; margin: 15px 0; text-align: center;'>
                <strong style='font-size: 18px;'>⚠️ {urgencyText} đến hạn!</strong>
            </div>
            
            <div style='background: white; padding: 20px; border-radius: 5px; border-left: 4px solid #667eea; margin: 20px 0;'>
                <h3 style='margin: 0 0 15px 0; color: #667eea;'>📌 Thông Tin Công Việc</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold; width: 30%;'>Tên công việc:</td>
                        <td style='padding: 8px 0;'>{task.Title}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Mô tả:</td>
                        <td style='padding: 8px 0;'>{task.Description ?? "Không có mô tả"}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Hạn hoàn thành:</td>
                        <td style='padding: 8px 0; color: {urgencyColor}; font-weight: bold;'>{task.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác định"}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Mức độ ưu tiên:</td>
                        <td style='padding: 8px 0;'>{GetPriorityText(task.Priority)}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Trạng thái:</td>
                        <td style='padding: 8px 0;'>{GetStatusText(task.Status)}</td>
                    </tr>
                </table>
            </div>
            
            <div style='text-align: center; margin: 20px 0;'>
                <p style='margin: 10px 0; font-size: 14px; color: #666;'>
                    Vui lòng hoàn thành công việc trước hạn để đảm bảo tiến độ dự án.
                </p>
                <a href='#' style='display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                    Xem Chi Tiết Công Việc
                </a>
            </div>
        </div>
        
        <div style='background: #343a40; color: #adb5bd; padding: 15px; border-radius: 0 0 10px 10px; text-align: center; font-size: 12px;'>
            <p style='margin: 0;'>📧 Email tự động từ Task Management System</p>
            <p style='margin: 5px 0 0 0;'>© 2025 NT106.P22.ANTT Team 4. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        // THÊM: Manual reminder email template
        private string GenerateManualReminderEmailBody(string recipientName, TaskItem task)
        {
            var urgencyColor = "#ff6b35"; // Orange cho manual reminder
            var timeRemaining = task.DueDate.HasValue ?
                (task.DueDate.Value - DateTime.Now).Days : 0;

            var timeText = timeRemaining > 0 ?
                $"Còn {timeRemaining} ngày" :
                timeRemaining == 0 ? "Hôm nay là hạn cuối" :
                $"Đã quá hạn {Math.Abs(timeRemaining)} ngày";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Nhắc nhở công việc từ Admin</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: linear-gradient(135deg, #ff6b35 0%, #f7931e 100%); color: white; padding: 20px; border-radius: 10px 10px 0 0;'>
            <h1 style='margin: 0; font-size: 24px;'>📧 Nhắc Nhở Từ Admin</h1>
        </div>
        
        <div style='background: #f8f9fa; padding: 20px; border: 1px solid #e9ecef;'>
            <p style='margin: 0 0 15px 0; font-size: 16px;'>Xin chào <strong>{recipientName}</strong>,</p>
            
            <div style='background: {urgencyColor}; color: white; padding: 10px 15px; border-radius: 5px; margin: 15px 0; text-align: center;'>
                <strong style='font-size: 18px;'>📢 Admin đã gửi nhắc nhở về công việc này</strong>
            </div>

            <div style='background: #fff3cd; color: #856404; padding: 10px 15px; border-radius: 5px; margin: 15px 0; text-align: center;'>
                <strong>⏰ {timeText}</strong>
            </div>
            
            <div style='background: white; padding: 20px; border-radius: 5px; border-left: 4px solid #ff6b35; margin: 20px 0;'>
                <h3 style='margin: 0 0 15px 0; color: #ff6b35;'>📌 Thông Tin Công Việc</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold; width: 30%;'>Tên công việc:</td>
                        <td style='padding: 8px 0;'>{task.Title}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Mô tả:</td>
                        <td style='padding: 8px 0;'>{task.Description ?? "Không có mô tả"}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Hạn hoàn thành:</td>
                        <td style='padding: 8px 0; color: {urgencyColor}; font-weight: bold;'>{task.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác định"}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Mức độ ưu tiên:</td>
                        <td style='padding: 8px 0;'>{GetPriorityText(task.Priority)}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; font-weight: bold;'>Trạng thái:</td>
                        <td style='padding: 8px 0;'>{GetStatusText(task.Status)}</td>
                    </tr>
                </table>
            </div>
            
            <div style='background: #e7f3ff; color: #0c5460; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                <p style='margin: 0; font-weight: bold;'>💡 Lưu ý từ Admin:</p>
                <p style='margin: 5px 0 0 0;'>Công việc này đã được Admin đặc biệt nhắc nhở. Vui lòng ưu tiên hoàn thành và báo cáo tiến độ.</p>
            </div>
            
            <div style='text-align: center; margin: 20px 0;'>
                <a href='#' style='display: inline-block; background: #ff6b35; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 0 5px;'>
                    Cập Nhật Tiến Độ
                </a>
                <a href='#' style='display: inline-block; background: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 0 5px;'>
                    Liên Hệ Admin
                </a>
            </div>
        </div>
        
        <div style='background: #343a40; color: #adb5bd; padding: 15px; border-radius: 0 0 10px 10px; text-align: center; font-size: 12px;'>
            <p style='margin: 0;'>📧 Email nhắc nhở từ Admin - Task Management System</p>
            <p style='margin: 5px 0 0 0;'>© 2025 NT106.P22.ANTT Team 4. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetPriorityText(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => "🟢 Thấp",
                TaskPriority.Medium => "🟡 Trung bình",
                TaskPriority.High => "🟠 Cao",
                TaskPriority.Critical => "🔴 Khẩn cấp",
                _ => "Không xác định"
            };
        }

        // SỬA: Sử dụng TaskItemStatus thay vì TaskStatus
        private string GetStatusText(TaskItemStatus status)
        {
            return status switch
            {
                TaskItemStatus.NotStarted => "⚪ Chưa bắt đầu",
                TaskItemStatus.InProgress => "🔵 Đang thực hiện",
                TaskItemStatus.Completed => "✅ Đã hoàn thành",
                TaskItemStatus.Canceled => "❌ Đã hủy",
                TaskItemStatus.Delayed => "🟡 Tạm hoãn",
                TaskItemStatus.Pending => "⏳ Chờ xử lý",
                _ => "Không xác định"
            };
        }

        private async Task<bool> SendEmailAsync(string recipientEmail, string recipientName, string subject, string body)
        {
            try
            {
                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_senderEmail, _senderPassword)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, _senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(recipientEmail, recipientName));

                await smtpClient.SendMailAsync(mailMessage);
                Debug.WriteLine($"Email gửi thành công đến {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gửi email: {ex.Message}");
                return false;
            }
        }
    }
}