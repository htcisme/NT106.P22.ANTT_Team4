using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Services
{
    public class GoogleCalendarService
    {
        private CalendarService _service;
        private readonly string[] _scopes = { CalendarService.Scope.Calendar };
        private readonly string _applicationName = "DoanKhoa Task Manager";
        private string _credentialsPath;
        private readonly string _tokenPath = "token.json";
        private bool _isAuthenticated = false;

        public GoogleCalendarService()
        {
            Debug.WriteLine("🔧 Initializing GoogleCalendarService...");
            FindCredentialsPath();
        }

        // ✅ STEP 1: Find credentials file in multiple locations
        private void FindCredentialsPath()
        {
            var possiblePaths = new[]
            {
                Path.Combine("Services", "credentials.json"),                    // Services folder
                "credentials.json",                                              // Root folder
                Path.Combine("..", "Services", "credentials.json"),             // Parent/Services
                Path.Combine("..", "credentials.json"),                         // Parent folder
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "credentials.json"), // App domain
                @"d:\Study\UIT\Mang\DOANMANG\NT106.P22.ANTT_Team4\DoanKhoaClient\Services\credentials.json" // Absolute
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        _credentialsPath = path;
                        Debug.WriteLine($"✅ Found credentials at: {Path.GetFullPath(path)}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error checking path {path}: {ex.Message}");
                }
            }

            Debug.WriteLine("❌ credentials.json not found in any location");
            _credentialsPath = null;
        }

        // ✅ STEP 2: Simple authentication
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                Debug.WriteLine("=== 🔐 GOOGLE CALENDAR AUTHENTICATION ===");

                // Check credentials file
                if (string.IsNullOrEmpty(_credentialsPath) || !File.Exists(_credentialsPath))
                {
                    ShowCredentialsError();
                    return false;
                }

                Debug.WriteLine($"📁 Using credentials: {_credentialsPath}");

                UserCredential credential;

                // OAuth2 flow
                using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    Debug.WriteLine("🌐 Starting OAuth2 flow (browser will open)...");

                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        _scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(_tokenPath, true));
                }

                Debug.WriteLine("✅ OAuth2 completed successfully");

                // Create service
                _service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _applicationName,
                });

                // Test API
                await TestCalendarAccess();

                _isAuthenticated = true;
                Debug.WriteLine("🎉 Authentication successful!");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Authentication failed: {ex.Message}");
                ShowAuthenticationError(ex.Message);
                return false;
            }
        }

        // ✅ STEP 3: Test calendar access
        private async Task TestCalendarAccess()
        {
            try
            {
                Debug.WriteLine("🧪 Testing Calendar API access...");

                var request = _service.CalendarList.List();
                request.MaxResults = 3;
                var calendars = await request.ExecuteAsync();

                Debug.WriteLine($"✅ Found {calendars.Items.Count} calendars");

                foreach (var cal in calendars.Items.Take(2))
                {
                    Debug.WriteLine($"  📅 {cal.Summary}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Calendar API test warning: {ex.Message}");
                // Don't fail authentication for API test issues
            }
        }

        // ✅ STEP 4: Create calendar event
        public async Task<string> CreateEventAsync(TaskItem task)
        {
            try
            {
                if (!_isAuthenticated && !await AuthenticateAsync())
                {
                    return null;
                }

                Debug.WriteLine($"📅 Creating event for: {task.Title}");

                var calendarEvent = new Event()
                {
                    Summary = $"📋 {task.Title}",
                    Description = BuildDescription(task),
                    Start = CreateEventDateTime(task.DueDate ?? DateTime.Now.AddDays(1)),
                    End = CreateEventDateTime((task.DueDate ?? DateTime.Now.AddDays(1)).AddHours(1)),
                    Location = "DoanKhoa Task Manager"
                };

                // Add attendee if email exists
                if (!string.IsNullOrEmpty(task.AssignedToEmail))
                {
                    calendarEvent.Attendees = new List<EventAttendee>
                    {
                        new EventAttendee
                        {
                            Email = task.AssignedToEmail,
                            DisplayName = task.AssignedToName ?? task.AssignedToEmail
                        }
                    };
                }

                // Add reminders
                calendarEvent.Reminders = new Event.RemindersData()
                {
                    UseDefault = false,
                    Overrides = new List<EventReminder>
                    {
                        new EventReminder { Method = "email", Minutes = 1440 }, // 1 day
                        new EventReminder { Method = "popup", Minutes = 60 }    // 1 hour
                    }
                };

                // Add metadata
                calendarEvent.ExtendedProperties = new Event.ExtendedPropertiesData()
                {
                    Private__ = new Dictionary<string, string>
                    {
                        { "taskId", task.Id },
                        { "source", "DoanKhoaTaskManager" }
                    }
                };

                // Insert event
                var request = _service.Events.Insert(calendarEvent, "primary");
                var response = await request.ExecuteAsync();

                Debug.WriteLine($"✅ Event created: {response.Id}");
                return response.Id;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to create event: {ex.Message}");
                return null;
            }
        }

        // ✅ STEP 5: Sync multiple tasks
        public async Task<CalendarSyncResult> SyncTasksAsync(List<TaskItem> tasks)
        {
            var result = new CalendarSyncResult { TotalTasks = tasks.Count };

            try
            {
                Debug.WriteLine($"🔄 Syncing {tasks.Count} tasks to calendar...");

                if (!_isAuthenticated && !await AuthenticateAsync())
                {
                    result.Errors.Add("Authentication failed");
                    return result;
                }

                foreach (var task in tasks)
                {
                    try
                    {
                        // Skip tasks without due dates
                        if (!task.DueDate.HasValue)
                        {
                            result.SkippedCount++;
                            result.SkippedTasks.Add($"{task.Title} - No due date");
                            Debug.WriteLine($"⏭️ Skipped: {task.Title} (no due date)");
                            continue;
                        }

                        // Create event
                        var eventId = await CreateEventAsync(task);

                        if (!string.IsNullOrEmpty(eventId))
                        {
                            result.CreatedCount++;
                            result.CreatedEvents.Add(new SyncedEvent
                            {
                                TaskId = task.Id,
                                TaskTitle = task.Title,
                                EventId = eventId
                            });
                            Debug.WriteLine($"✅ Synced: {task.Title}");
                        }
                        else
                        {
                            result.FailedCount++;
                            result.Errors.Add($"Failed to create event for: {task.Title}");
                            Debug.WriteLine($"❌ Failed: {task.Title}");
                        }

                        // Rate limiting
                        await Task.Delay(300);
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"{task.Title}: {ex.Message}");
                        Debug.WriteLine($"❌ Error syncing {task.Title}: {ex.Message}");
                    }
                }

                result.SuccessCount = result.CreatedCount + result.UpdatedCount;

                Debug.WriteLine($"📊 Sync completed - Created: {result.CreatedCount}, Failed: {result.FailedCount}, Skipped: {result.SkippedCount}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Sync failed: {ex.Message}");
                result.Errors.Add($"Sync failed: {ex.Message}");
                return result;
            }
        }

        // ✅ HELPER: Build event description
        private string BuildDescription(TaskItem task)
        {
            var desc = $"📋 Công việc từ DoanKhoa Task Manager\n\n";
            desc += $"Tên: {task.Title}\n";
            desc += $"Mô tả: {task.Description ?? "Không có mô tả"}\n";
            desc += $"Người thực hiện: {task.AssignedToName ?? task.AssignedToEmail ?? "Chưa phân công"}\n";
            desc += $"Trạng thái: {GetStatusText(task.Status)}\n";
            desc += $"Độ ưu tiên: {GetPriorityText(task.Priority)}\n";

            if (task.DueDate.HasValue)
            {
                desc += $"Hạn chót: {task.DueDate.Value:dd/MM/yyyy HH:mm}\n";
            }

            desc += $"\n🔗 Quản lý trong DoanKhoa Task Manager";
            return desc;
        }

        // ✅ HELPER: Create EventDateTime
        private EventDateTime CreateEventDateTime(DateTime dateTime)
        {
            return new EventDateTime()
            {
                DateTime = dateTime,
                TimeZone = "Asia/Ho_Chi_Minh"
            };
        }

        // ✅ HELPER: Get status text
        private string GetStatusText(TaskItemStatus status)
        {
            return status switch
            {
                TaskItemStatus.NotStarted => "Chưa bắt đầu",
                TaskItemStatus.InProgress => "Đang thực hiện",
                TaskItemStatus.Completed => "Đã hoàn thành",
                TaskItemStatus.Canceled => "Đã hủy",
                _ => status.ToString()
            };
        }

        // ✅ HELPER: Get priority text  
        private string GetPriorityText(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => "Thấp",
                TaskPriority.Medium => "Trung bình",
                TaskPriority.High => "Cao",
                TaskPriority.Critical => "Khẩn cấp",
                _ => priority.ToString()
            };
        }

        // ✅ ERROR HANDLERS: Show user-friendly errors
        private void ShowCredentialsError()
        {
            var message = "❌ Không tìm thấy file credentials.json!\n\n" +
                         "🔧 Hướng dẫn setup:\n" +
                         "1. Vào Google Cloud Console\n" +
                         "2. Tạo OAuth 2.0 credentials\n" +
                         "3. Download credentials.json\n" +
                         "4. Copy vào thư mục:\n" +
                         "   • DoanKhoaClient/Services/\n" +
                         "   • DoanKhoaClient/\n\n" +
                         "📍 Hiện tại đang tìm tại:\n";

            var paths = new[]
            {
                Path.Combine("Services", "credentials.json"),
                "credentials.json"
            };

            foreach (var path in paths)
            {
                message += $"   • {Path.GetFullPath(path)}\n";
            }

            MessageBox.Show(message, "Credentials Required", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowAuthenticationError(string error)
        {
            var message = $"❌ Google Calendar authentication thất bại!\n\n" +
                         $"Lỗi: {error}\n\n" +
                         "🔧 Solutions:\n" +
                         "• Kiểm tra Internet connection\n" +
                         "• Thử xóa thư mục token.json\n" +
                         "• Verify Google Cloud Console settings\n" +
                         "• Enable Google Calendar API\n" +
                         "• Login lại Google account trong browser";

            MessageBox.Show(message, "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ✅ PUBLIC: Simple test method
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Debug.WriteLine("🧪 Testing Google Calendar connection...");
                return await AuthenticateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Connection test failed: {ex.Message}");
                return false;
            }
        }

        // ✅ CLEANUP: Dispose resources
        public void Dispose()
        {
            _service?.Dispose();
            _isAuthenticated = false;
            Debug.WriteLine("🧹 GoogleCalendarService disposed");
        }
    }

    // ✅ SIMPLE MODELS: Sync results
    public class CalendarSyncResult
    {
        public int TotalTasks { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public int SuccessCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> SkippedTasks { get; set; } = new List<string>();
        public List<SyncedEvent> CreatedEvents { get; set; } = new List<SyncedEvent>();
    }

    public class SyncedEvent
    {
        public string TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string EventId { get; set; }
    }
}