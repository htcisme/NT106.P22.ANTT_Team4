using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Services
{
    public class TaskReminderService
    {
        private readonly TaskService _taskService;
        private readonly EmailService _emailService;
        private readonly DispatcherTimer _reminderTimer;
        private readonly List<string> _sentReminders; // Tránh gửi email trùng lặp

        public TaskReminderService(TaskService taskService, EmailService emailService)
        {
            _taskService = taskService;
            _emailService = emailService;
            _sentReminders = new List<string>();

            // Thiết lập timer để check mỗi giờ
            _reminderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(1) // Check mỗi giờ
            };
            _reminderTimer.Tick += OnReminderTimerTick;
        }

        public void StartReminderService()
        {
            Debug.WriteLine("===== TASK REMINDER SERVICE STARTED =====");
            _reminderTimer.Start();

            // Chạy check ngay lập tức khi start
            _ = Task.Run(CheckAndSendRemindersAsync);
        }

        public void StopReminderService()
        {
            Debug.WriteLine("===== TASK REMINDER SERVICE STOPPED =====");
            _reminderTimer.Stop();
        }

        private async void OnReminderTimerTick(object sender, EventArgs e)
        {
            await CheckAndSendRemindersAsync();
        }

        private async Task CheckAndSendRemindersAsync()
        {
            try
            {
                Debug.WriteLine($"===== CHECKING TASK REMINDERS - {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");

                // Lấy tất cả TaskItems đang active
                var allTasks = await _taskService.GetAllActiveTaskItemsAsync();

                if (allTasks?.Any() != true)
                {
                    Debug.WriteLine("Không có TaskItems nào để kiểm tra.");
                    return;
                }

                var now = DateTime.Now;
                var tasksNeedingReminder = new List<(TaskItem task, int daysRemaining)>();

                foreach (var task in allTasks)
                {
                    // THAY ĐỔI: Sử dụng TaskItemStatus thay vì TaskStatus
                    if (task.Status == TaskItemStatus.Completed)
                        continue;

                    // THAY ĐỔI: Kiểm tra DueDate có null không
                    if (task.DueDate == null)
                        continue;

                    var timeRemaining = task.DueDate.Value - now;
                    var daysRemaining = (int)Math.Ceiling(timeRemaining.TotalDays);

                    // Nhắc nhở khi còn 2 ngày hoặc 1 ngày
                    if (daysRemaining == 2 || daysRemaining == 1)
                    {
                        var reminderKey = $"{task.Id}_{daysRemaining}day";

                        // Kiểm tra đã gửi reminder này chưa
                        if (!_sentReminders.Contains(reminderKey))
                        {
                            tasksNeedingReminder.Add((task, daysRemaining));
                            _sentReminders.Add(reminderKey);

                            Debug.WriteLine($"Task '{task.Title}' cần nhắc nhở - còn {daysRemaining} ngày");
                        }
                    }

                    // Clear old reminders (sau khi qua hạn)
                    if (daysRemaining < 0)
                    {
                        _sentReminders.RemoveAll(r => r.StartsWith($"{task.Id}_"));
                    }
                }

                // Gửi reminders
                foreach (var (task, daysRemaining) in tasksNeedingReminder)
                {
                    await SendTaskReminderAsync(task, daysRemaining);
                }

                Debug.WriteLine($"Đã kiểm tra {allTasks.Count} tasks, gửi {tasksNeedingReminder.Count} reminders");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi kiểm tra task reminders: {ex.Message}");
            }
        }

        private async Task SendTaskReminderAsync(TaskItem task, int daysRemaining)
        {
            try
            {
                // Lấy thông tin người thực hiện task
                var recipientEmail = await GetTaskAssigneeEmailAsync(task);
                var recipientName = task.AssigneeName ?? task.AssignedToName ?? "Người thực hiện";

                if (string.IsNullOrEmpty(recipientEmail))
                {
                    Debug.WriteLine($"Không có email cho task '{task.Title}', bỏ qua reminder");
                    return;
                }

                var success = await _emailService.SendTaskReminderEmailAsync(
                    recipientEmail, recipientName, task, daysRemaining);

                if (success)
                {
                    Debug.WriteLine($"✅ Gửi reminder thành công cho task '{task.Title}' đến {recipientEmail}");
                }
                else
                {
                    Debug.WriteLine($"❌ Gửi reminder thất bại cho task '{task.Title}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gửi reminder cho task '{task.Title}': {ex.Message}");
            }
        }

        private async Task<string> GetTaskAssigneeEmailAsync(TaskItem task)
        {
            try
            {
                // Ưu tiên 1: Nếu TaskItem đã có AssigneeEmail (property mới)
                if (!string.IsNullOrEmpty(task.AssigneeEmail))
                {
                    return task.AssigneeEmail;
                }

                // Ưu tiên 2: Nếu có AssignedToId, lấy email từ User service
                if (!string.IsNullOrEmpty(task.AssignedToId)) // SỬA: AssigneeId → AssignedToId
                {
                    // TODO: Implement UserService để lấy email theo UserId
                    // var userService = new UserService();
                    // var user = await userService.GetUserByIdAsync(task.AssignedToId);
                    // if (user != null && !string.IsNullOrEmpty(user.Email))
                    // {
                    //     return user.Email;
                    // }

                    Debug.WriteLine($"TaskItem '{task.Title}' có AssignedToId '{task.AssignedToId}' nhưng UserService chưa được implement");
                }

                // Ưu tiên 3: Fallback strategies

                // 3a. Lấy từ ExecutorEmail của TaskProgram
                if (!string.IsNullOrEmpty(task.ProgramId))
                {
                    try
                    {
                        var program = await _taskService.GetTaskProgramByIdAsync(task.ProgramId);
                        if (program != null && !string.IsNullOrEmpty(program.ExecutorName))
                        {
                            // Có thể tạo email từ ExecutorName nếu có pattern
                            // Ví dụ: "Trần Văn Nam" → "tran.van.nam@example.com"
                            var generatedEmail = GenerateEmailFromName(program.ExecutorName);
                            if (!string.IsNullOrEmpty(generatedEmail))
                            {
                                Debug.WriteLine($"Generated email từ Program Executor: {generatedEmail}");
                                return generatedEmail;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi lấy email từ TaskProgram: {ex.Message}");
                    }
                }

                // 3b. Sử dụng hardcoded email cho test (temporary)
                if (!string.IsNullOrEmpty(task.AssignedToName))
                {
                    var testEmail = GetTestEmailForUser(task.AssignedToName);
                    if (!string.IsNullOrEmpty(testEmail))
                    {
                        Debug.WriteLine($"Sử dụng test email cho '{task.AssignedToName}': {testEmail}");
                        return testEmail;
                    }
                }

                Debug.WriteLine($"Không tìm thấy email cho task '{task.Title}' assigned to '{task.AssignedToName}'");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy email assignee: {ex.Message}");
                return null;
            }
        }

        // THÊM: Helper method để generate email từ tên
        private string GenerateEmailFromName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            try
            {
                // Convert "Trần Văn Nam" → "tran.van.nam@doankhoa.uit.edu.vn"
                var normalizedName = fullName.ToLower()
                    .Replace(" ", ".")
                    .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                    .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                    .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                    .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                    .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                    .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                    .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                    .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                    .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                    .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                    .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                    .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                    .Replace("đ", "d");

                return $"{normalizedName}@doankhoa.uit.edu.vn";
            }
            catch
            {
                return null;
            }
        }

        // THÊM: Helper method để map test emails
        private string GetTestEmailForUser(string assignedToName)
        {
            if (string.IsNullOrEmpty(assignedToName))
                return null;

            // Hardcoded mapping cho testing
            var testEmailMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Trần Văn Nam", "tran.van.nam@test.com" },
                { "Hoàng Bảo Phước", "hoang.bao.phuoc@test.com" },
                { "Huỳnh Ngọc Ngân Tuyền", "huynh.ngan.tuyen@test.com" },
                { "Test User", "test.user@test.com" },
                { "Admin", "admin@test.com" }
            };

            return testEmailMappings.TryGetValue(assignedToName, out var email) ? email : null;
        }

        // THÊM: Method để manually trigger reminder check (cho testing)
        public async Task TriggerReminderCheckAsync()
        {
            Debug.WriteLine("===== MANUAL REMINDER CHECK TRIGGERED =====");
            await CheckAndSendRemindersAsync();
        }
    }
}