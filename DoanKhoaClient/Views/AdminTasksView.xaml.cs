using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Extensions;
using DoanKhoaClient.Services;
using DoanKhoaClient.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

namespace DoanKhoaClient.Views
{
    public partial class AdminTasksView : Window
    {
        private AdminTasksViewModel _viewModel;
        private bool _isDarkMode;
        private bool isAdminSubmenuOpen;
        private readonly TaskService _taskService;
        private readonly GoogleCalendarService _googleCalendarService;

        public AdminTasksView()
        {
            InitializeComponent();

            // Kiểm tra quyền truy cập
            AccessControl.CheckAdminAccess(this);

            _viewModel = new AdminTasksViewModel();
            DataContext = _viewModel;
            ThemeManager.ApplyTheme(Admin_Task_Background);

            // ✅ THÊM: Initialize TaskService
            _taskService = new TaskService();

            // Thêm xử lý hướng dẫn và kiểm tra tài nguyên
            Loaded += AdminTasksView_Loaded;
            Admin_Task_iUsers.SetupAsUserAvatar();

            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }
            _googleCalendarService = new GoogleCalendarService();

        }

        // ✅ THÊM: Reminder Button Click Handler
        private async void ReminderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("===== ADMIN REMINDER BUTTON CLICKED =====");

                // Show loading state
                SetReminderLoadingState(true);

                // Get all task items from all sessions/programs
                var allTaskItems = await GetAllTaskItemsFromSessionsAsync();

                Debug.WriteLine($"Total task items found: {allTaskItems.Count}");

                // Filter tasks that need reminders
                var tasksNeedingReminders = FilterTasksForReminders(allTaskItems);

                Debug.WriteLine($"Tasks needing reminders: {tasksNeedingReminders.Count}");

                if (!tasksNeedingReminders.Any())
                {
                    ShowStatus("✅ Không có công việc nào cần nhắc nhở", "#28a745");
                    MessageBox.Show("Không có công việc nào cần nhắc nhở hiện tại.\n\n" +
                        "📋 Các công việc đã hoàn thành hoặc bị hủy sẽ không được gửi nhắc nhở.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show confirmation
                var confirmMessage = BuildConfirmationMessage(tasksNeedingReminders);
                var result = MessageBox.Show(confirmMessage,
                    "Xác nhận gửi nhắc nhở",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Send reminders
                    var reminderResult = await SendRemindersAsync(tasksNeedingReminders);
                    ShowReminderResults(reminderResult);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in AdminTasksView ReminderButton_Click: {ex.Message}");
                ShowStatus($"❌ Lỗi: {ex.Message}", "#dc3545");
                MessageBox.Show($"❌ Lỗi khi gửi nhắc nhở: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetReminderLoadingState(false);
            }
        }

        // ✅ THÊM: Get all task items from all sessions
        private async Task<List<TaskItem>> GetAllTaskItemsFromSessionsAsync()
        {
            var allTaskItems = new List<TaskItem>();

            try
            {
                Debug.WriteLine("===== GETTING ALL TASK ITEMS - SIMPLIFIED =====");

                // ✅ STEP 1: Get all TaskPrograms directly from API
                var taskProgramService = new TaskProgramService();
                var allPrograms = await taskProgramService.GetAllTaskProgramsAsync();

                Debug.WriteLine($"Found {allPrograms?.Count ?? 0} TaskPrograms from API");

                if (allPrograms != null && allPrograms.Count > 0)
                {
                    foreach (var program in allPrograms)
                    {
                        Debug.WriteLine($"\n--- Processing Program: {program.Name} (ID: {program.Id}) ---");
                        Debug.WriteLine($"Type: {program.Type}, Status: {program.Status}");
                        Debug.WriteLine($"SessionId: {program.SessionId}");
                        Debug.WriteLine($"ExecutorName: {program.ExecutorName}");

                        try
                        {
                            // ✅ STEP 2: Get TaskItems for each program using program.Id
                            var taskItems = await _taskService.GetTaskItemsByProgramIdAsync(program.Id);

                            Debug.WriteLine($"Retrieved {taskItems?.Count ?? 0} TaskItems from program '{program.Name}'");

                            if (taskItems != null && taskItems.Count > 0)
                            {
                                allTaskItems.AddRange(taskItems);

                                // Debug: Show sample tasks
                                foreach (var task in taskItems.Take(2))
                                {
                                    Debug.WriteLine($"   - {task.Title} (Status: {task.Status}, Due: {task.DueDate?.ToString("MM/dd") ?? "NULL"}, Email: '{task.AssignedToEmail ?? "NULL"}')");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"⚠️ No TaskItems found for program '{program.Name}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"❌ Error getting TaskItems for program {program.Name}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("❌ No TaskPrograms found from API");
                    MessageBox.Show("❌ Không tìm thấy TaskPrograms nào từ API!\n\n" +
                                   "Possible issues:\n" +
                                   "• API server không chạy\n" +
                                   "• Database trống\n" +
                                   "• Network connection lỗi\n\n" +
                                   "Hãy kiểm tra server và thử lại.",
                                   "No Programs Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ CRITICAL Error in GetAllTaskItemsFromSessionsAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show($"❌ Lỗi khi lấy TaskItems:\n\n{ex.Message}\n\n" +
                               "Hãy kiểm tra:\n" +
                               "• API server có chạy không\n" +
                               "• Network connection\n" +
                               "• Authentication",
                               "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Remove duplicates
            var uniqueTaskItems = allTaskItems
                .Where(t => t != null && !string.IsNullOrEmpty(t.Id))
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .ToList();

            Debug.WriteLine($"\n===== FINAL RESULT =====");
            Debug.WriteLine($"Total TaskItems collected: {allTaskItems.Count}");
            Debug.WriteLine($"Unique TaskItems after deduplication: {uniqueTaskItems.Count}");

            return uniqueTaskItems;
        }

        private async void CalendarSyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("📅 Calendar Sync button clicked");

                // Show loading
                SetCalendarSyncLoadingState(true);
                ShowStatus("🔄 Getting tasks for calendar sync...", "#17a2b8");

                // Get all task items
                var allTaskItems = await GetAllTaskItemsFromSessionsAsync();
                Debug.WriteLine($"Retrieved {allTaskItems?.Count ?? 0} total tasks");

                if (allTaskItems == null || allTaskItems.Count == 0)
                {
                    ShowStatus("⚠️ No tasks found for sync", "#ffc107");
                    MessageBox.Show("❌ Không tìm thấy tasks nào để đồng bộ!\n\n" +
                                   "Possible reasons:\n" +
                                   "• No TaskPrograms exist\n" +
                                   "• No TaskItems in programs\n" +
                                   "• API connection issues\n\n" +
                                   "Hãy kiểm tra TaskPrograms và TaskItems trước.",
                                   "No Tasks Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Filter tasks for calendar sync
                var eligibleTasks = allTaskItems.Where(t =>
                    t.DueDate.HasValue &&
                    t.Status != TaskItemStatus.Completed &&
                    t.Status != TaskItemStatus.Canceled
                ).ToList();

                Debug.WriteLine($"Found {eligibleTasks.Count} eligible tasks for calendar sync");

                if (eligibleTasks.Count == 0)
                {
                    ShowStatus("⚠️ No eligible tasks for sync", "#ffc107");
                    MessageBox.Show($"📊 Không có tasks phù hợp cho Calendar Sync!\n\n" +
                                   $"Từ {allTaskItems.Count} tasks:\n" +
                                   $"• {allTaskItems.Count(t => !t.DueDate.HasValue)} tasks không có due date\n" +
                                   $"• {allTaskItems.Count(t => t.Status == TaskItemStatus.Completed)} tasks đã hoàn thành\n" +
                                   $"• {allTaskItems.Count(t => t.Status == TaskItemStatus.Canceled)} tasks đã hủy\n\n" +
                                   $"✅ Điều kiện sync: có due date + chưa hoàn thành/hủy",
                                   "No Eligible Tasks", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show confirmation
                var confirmMessage = BuildSimpleConfirmation(eligibleTasks);
                var result = MessageBox.Show(confirmMessage,
                    "Confirm Calendar Sync",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ShowStatus("🔄 Syncing with Google Calendar...", "#17a2b8");

                    // Perform sync
                    var syncResult = await _googleCalendarService.SyncTasksAsync(eligibleTasks);

                    // Show results
                    ShowSyncResults(syncResult);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Calendar sync error: {ex.Message}");
                ShowStatus($"❌ Sync failed: {ex.Message}", "#dc3545");
                MessageBox.Show($"❌ Calendar sync error:\n\n{ex.Message}",
                    "Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetCalendarSyncLoadingState(false);
            }
        }

        // ✅ SIMPLE: Build confirmation message
        private string BuildSimpleConfirmation(List<TaskItem> tasks)
        {
            var message = $"📅 Google Calendar Sync\n\n";
            message += $"Sẽ tạo {tasks.Count} calendar events cho:\n\n";

            foreach (var task in tasks.Take(5))
            {
                var dueDateStr = task.DueDate?.ToString("dd/MM HH:mm") ?? "TBD";
                var assignee = task.AssignedToName ?? task.AssignedToEmail ?? "Unassigned";
                message += $"📋 {task.Title}\n";
                message += $"   ⏰ {dueDateStr} | 👤 {assignee}\n\n";
            }

            if (tasks.Count > 5)
            {
                message += $"... và {tasks.Count - 5} tasks khác\n\n";
            }

            message += $"🔔 Mỗi event sẽ có:\n";
            message += $"• Email reminder 1 ngày trước\n";
            message += $"• Popup reminder 1 giờ trước\n";
            message += $"• Attendee là người được assign\n\n";
            message += $"❓ Tiếp tục sync với Google Calendar?";

            return message;
        }

        // ✅ SIMPLE: Show sync results  
        private void ShowSyncResults(CalendarSyncResult result)
        {
            var message = $"✅ Calendar Sync Completed!\n\n";
            message += $"📊 Results:\n";
            message += $"• Created: {result.CreatedCount} events\n";
            message += $"• Updated: {result.UpdatedCount} events\n";
            message += $"• Skipped: {result.SkippedCount} tasks\n";
            message += $"• Failed: {result.FailedCount} tasks\n";

            if (result.Errors.Count > 0)
            {
                message += $"\n❌ Errors:\n";
                message += string.Join("\n", result.Errors.Take(3));
                if (result.Errors.Count > 3)
                {
                    message += $"\n... và {result.Errors.Count - 3} errors khác";
                }
            }

            if (result.CreatedEvents.Count > 0)
            {
                message += $"\n\n🎉 {result.CreatedEvents.Count} events đã được tạo thành công!";
            }

            var icon = result.FailedCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning;
            MessageBox.Show(message, "Sync Results", MessageBoxButton.OK, icon);

            if (result.CreatedCount > 0)
            {
                ShowStatus($"✅ Synced {result.CreatedCount} tasks to calendar", "#28a745");
            }
            else
            {
                ShowStatus($"⚠️ No tasks synced", "#ffc107");
            }
        }

        // ✅ ADD: Test connection button
        private async void TestGoogleConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowStatus("🧪 Testing Google Calendar connection...", "#17a2b8");

                var success = await _googleCalendarService.TestConnectionAsync();

                if (success)
                {
                    ShowStatus("✅ Google Calendar connected!", "#28a745");
                    MessageBox.Show("✅ Google Calendar connection successful!\n\n" +
                                   "You can now use Calendar Sync feature.",
                                   "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowStatus("❌ Connection failed", "#dc3545");
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Test failed: {ex.Message}", "#dc3545");
                MessageBox.Show($"❌ Connection test failed:\n\n{ex.Message}",
                               "Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ ADD: Calendar sync loading state
        private void SetCalendarSyncLoadingState(bool isLoading)
        {
            CalendarSyncButtonBorder.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            CalendarSyncLoadingBorder.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }
        private async Task<TaskProgram> GetTaskProgramFromSessionAsync(TaskSession session)
        {
            try
            {
                MessageBox.Show($"Getting TaskProgram for session: {session.Name}");
                MessageBox.Show($"Session Type: {session.Type}");
                MessageBox.Show($"Session Id: '{session.Id ?? "NULL"}'");

                // ✅ METHOD 1: If session has direct TaskProgram reference
                if (!string.IsNullOrEmpty(session.Id))
                {
                    Debug.WriteLine($"Session has Id: {session.Id}");
                    var taskProgramService = new TaskProgramService();
                    var program = await taskProgramService.GetTaskProgramByIdAsync(session.Id);

                    if (program != null)
                    {
                        Debug.WriteLine($"✅ Found TaskProgram by ID: {program.Name}");
                        return program;
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ TaskProgram not found for ID: {session.Id}");
                    }
                }

                // ✅ METHOD 2: Get TaskProgram by session type mapping
                var programId = GetProgramIdFromSessionType(session.Type);
                if (!string.IsNullOrEmpty(programId))
                {
                    Debug.WriteLine($"Mapped session type {session.Type} to programId: {programId}");
                    var taskProgramService = new TaskProgramService();
                    var program = await taskProgramService.GetTaskProgramByIdAsync(programId);

                    if (program != null)
                    {
                        Debug.WriteLine($"✅ Found TaskProgram by type mapping: {program.Name}");

                        // ✅ UPDATE: Set Id for future use
                        session.Id = program.Id;

                        return program;
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ TaskProgram not found for mapped ID: {programId}");
                    }
                }

                // ✅ METHOD 3: Search TaskProgram by session name
                Debug.WriteLine("Trying to find TaskProgram by name matching...");
                var allPrograms = await GetAllTaskProgramsAsync();
                Debug.WriteLine($"Available programs: {string.Join(", ", allPrograms.Select(p => $"{p.Name} (ID: {p.Id})"))}");

                var matchingProgram = allPrograms?.FirstOrDefault(p =>
                    p.Name.Contains(session.Name, StringComparison.OrdinalIgnoreCase) ||
                    session.Name.Contains(p.Name, StringComparison.OrdinalIgnoreCase)
                );

                if (matchingProgram != null)
                {
                    Debug.WriteLine($"✅ Found TaskProgram by name matching: {matchingProgram.Name}");

                    // ✅ UPDATE: Set Id for future use
                    session.Id = matchingProgram.Id;

                    return matchingProgram;
                }

                // ✅ METHOD 4: Create default TaskProgram if none found
                Debug.WriteLine("No TaskProgram found, creating default...");
                var defaultProgram = CreateDefaultTaskProgramForSession(session);
                if (defaultProgram != null)
                {
                    Debug.WriteLine($"✅ Created default TaskProgram: {defaultProgram.Name}");
                    session.Id = defaultProgram.Id;
                    return defaultProgram;
                }

                Debug.WriteLine($"❌ No TaskProgram found for session: {session.Name}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting TaskProgram for session {session.Name}: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        // ✅ ADD: Create default TaskProgram for session
        private TaskProgram CreateDefaultTaskProgramForSession(TaskSession session)
        {
            try
            {
                var programId = GetProgramIdFromSessionType(session.Type);
                var programName = GetProgramNameFromSessionType(session.Type);

                if (string.IsNullOrEmpty(programId) || string.IsNullOrEmpty(programName))
                {
                    programId = session.Type.ToString().ToLower();
                    programName = $"Chương trình {session.Type}";
                }

                return new TaskProgram
                {
                    Id = programId,
                    Name = programName,
                    Description = $"Chương trình mặc định cho {session.Name}",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(3),
                    Type = (ProgramType)session.Type,
                    Status = ProgramStatus.InProgress,
                    SessionId = session.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error creating default TaskProgram: {ex.Message}");
                return null;
            }
        }

        // ✅ ADD: Get program name from session type
        private string GetProgramNameFromSessionType(TaskSessionType sessionType)
        {
            return sessionType switch
            {
                TaskSessionType.Design => "Ban Thiết kế",
                TaskSessionType.Event => "Ban Truyền thông và Sự kiện",
                TaskSessionType.Study => "Ban Học tập",
                _ => $"Chương trình {sessionType}"
            };
        }
        private string GetProgramIdFromSessionType(TaskSessionType sessionType)
        {
            return sessionType switch
            {
                TaskSessionType.Design => "design",
                TaskSessionType.Event => "event",
                TaskSessionType.Study => "study",
                _ => null
            };
        }
        // ✅ NEW: Get TaskItems from TaskProgram
        private async Task<List<TaskItem>> GetTaskItemsFromTaskProgramAsync(TaskProgram taskProgram)
        {
            try
            {
                Debug.WriteLine($"Getting TaskItems from TaskProgram: {taskProgram.Name} (ID: {taskProgram.Id})");

                // ✅ METHOD 1: Use existing TaskService method
                var taskItems = await _taskService.GetTaskItemsByProgramIdAsync(taskProgram.Id);
                MessageBox.Show($"Retrieved {taskItems.Count} TaskItems from TaskProgram {taskProgram.Name}");
                if (taskItems != null && taskItems.Count > 0)
                {
                    Debug.WriteLine($"✅ Retrieved {taskItems.Count} TaskItems from TaskProgram");
                    return taskItems;
                }

                // ✅ METHOD 2: If TaskProgram has direct TaskItems collection
                if (taskProgram.TaskItems != null && taskProgram.TaskItems.Count > 0)
                {
                    Debug.WriteLine($"✅ Using TaskProgram.TaskItems collection: {taskProgram.TaskItems.Count} items");
                    return taskProgram.TaskItems.ToList();
                }

                Debug.WriteLine($"⚠️ No TaskItems found in TaskProgram: {taskProgram.Name}");
                return new List<TaskItem>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting TaskItems from TaskProgram {taskProgram.Name}: {ex.Message}");
                return new List<TaskItem>();
            }
        }
        // ✅ THÊM: Get program ID from session type
        private string GetProgramIdFromSession(TaskSession session)
        {
            // Map session types to program IDs
            return session.Type switch
            {
                TaskSessionType.Design => "design-program-id",
                TaskSessionType.Event => "event-program-id",
                TaskSessionType.Study => "study-program-id",
                _ => session.Id // Use session ID as fallback
            };
        }

        // ✅ THÊM: Get all task programs (fallback method)
        private async Task<List<TaskProgram>> GetAllTaskProgramsAsync()
        {
            var programs = new List<TaskProgram>();

            try
            {
                var taskProgramService = new TaskProgramService();
                var allPrograms = await taskProgramService.GetAllTaskProgramsAsync();

                // Filter for main teams
                programs = allPrograms.Where(p =>
                    p.Name.Contains("Thiết kế") ||
                    p.Name.Contains("Design") ||
                    p.Name.Contains("Truyền thông") ||
                    p.Name.Contains("Event") ||
                    p.Name.Contains("Học tập") ||
                    p.Name.Contains("Study")
                ).ToList();

                Debug.WriteLine($"Filtered programs: {string.Join(", ", programs.Select(p => p.Name))}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting task programs: {ex.Message}");

                // Fallback: Create default programs
                programs = new List<TaskProgram>
                {
                    new TaskProgram { Id = "design", Name = "Ban Thiết kế" },
                    new TaskProgram { Id = "event", Name = "Ban Truyền thông và Sự kiện" },
                    new TaskProgram { Id = "study", Name = "Ban Học tập" }
                };
            }

            return programs;
        }

        // ✅ THÊM: Filter tasks that need reminders
        private List<TaskItem> FilterTasksForReminders(List<TaskItem> allTasks)
        {
            var now = DateTime.Now;
            Debug.WriteLine($"===== FILTERING TASKS FOR REMINDERS =====");
            Debug.WriteLine($"Current time: {now:yyyy-MM-dd HH:mm:ss}");
            Debug.WriteLine($"Total tasks to check: {allTasks.Count}");
            Debug.WriteLine("");

            var filteredTasks = new List<TaskItem>();

            foreach (var task in allTasks)
            {
                Debug.WriteLine($"--- Checking Task: {task.Title} ---");
                Debug.WriteLine($"ID: {task.Id}");
                Debug.WriteLine($"Status: {task.Status}");
                Debug.WriteLine($"DueDate: {task.DueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL"}");
                Debug.WriteLine($"AssignedToEmail: '{task.AssignedToEmail ?? "NULL"}'");
                // ✅ CHECK 1: DueDate
                if (!task.DueDate.HasValue)
                {
                    Debug.WriteLine("❌ SKIPPED: No due date");
                    Debug.WriteLine("");
                    continue;
                }

                // ✅ CHECK 2: Status
                if (task.Status == TaskItemStatus.Canceled)
                {
                    Debug.WriteLine("❌ SKIPPED: Task is CANCELED");
                    Debug.WriteLine("");
                    continue;
                }

                if (task.Status == TaskItemStatus.Completed)
                {
                    Debug.WriteLine("❌ SKIPPED: Task is COMPLETED");
                    Debug.WriteLine("");
                    continue;
                }

                // ✅ CHECK 3: Email
                if (string.IsNullOrWhiteSpace(task.AssignedToEmail))
                {
                    Debug.WriteLine("❌ SKIPPED: No assigned email");
                    Debug.WriteLine("");
                    continue;
                }

                // ✅ CHECK 4: Calculate days
                var dueDate = task.DueDate.Value;
                var timeSpan = dueDate - now;
                var daysUntilDue = timeSpan.Days;
                var hoursUntilDue = timeSpan.TotalHours;


                Debug.WriteLine($"Due date: {dueDate:yyyy-MM-dd HH:mm:ss}");
                Debug.WriteLine($"Days until due: {daysUntilDue}");
                Debug.WriteLine($"Hours until due: {hoursUntilDue:F1}");

                // ✅ CHECK 5: Overdue tasks
                if (dueDate < now)
                {
                    var daysOverdue = (now - dueDate).Days;
                    Debug.WriteLine($"✅ INCLUDED: OVERDUE by {daysOverdue} days");
                    filteredTasks.Add(task);
                    Debug.WriteLine("");
                    continue;
                }

                // ✅ CHECK 6: Tasks due within 3 days
                if (daysUntilDue <= 3 && daysUntilDue >= 0)
                {
                    Debug.WriteLine($"✅ INCLUDED: Due in {daysUntilDue} days (within 3-day threshold)");
                    filteredTasks.Add(task);
                    Debug.WriteLine("");
                    continue;
                }

                // ✅ CHECK 7: Tasks due within 72 hours (more precise)
                if (hoursUntilDue <= 72 && hoursUntilDue >= 0)
                {
                    Debug.WriteLine($"✅ INCLUDED: Due in {hoursUntilDue:F1} hours (within 72-hour threshold)");
                    filteredTasks.Add(task);
                    Debug.WriteLine("");
                    continue;
                }

                Debug.WriteLine($"❌ SKIPPED: Due in {daysUntilDue} days (beyond 3-day threshold)");
                Debug.WriteLine("");
            }

            Debug.WriteLine($"===== FILTER RESULT =====");
            Debug.WriteLine($"Total filtered: {filteredTasks.Count}/{allTasks.Count}");
            foreach (var task in filteredTasks)
            {
                var daysUntil = task.DueDate.HasValue ? (task.DueDate.Value - now).Days : 0;
                Debug.WriteLine($"  ✅ {task.Title} - Due in {daysUntil} days ({task.DueDate:MM/dd})");
            }
            Debug.WriteLine("");

            return filteredTasks;
        }

        // ✅ THÊM: Build confirmation message
        private string BuildConfirmationMessage(List<TaskItem> tasks)
        {
            var now = DateTime.Now;
            var overdueCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now);
            var upcomingCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value >= now);
            var todayCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == now.Date);

            var message = $"🔔 **Admin Panel - Gửi Nhắc Nhở Hàng Loạt**\n\n";
            message += $"📊 **Tổng quan: {tasks.Count} công việc cần nhắc nhở**\n\n";

            // ✅ ADD: Detailed breakdown
            message += $"📈 **Phân loại theo thời gian:**\n";
            if (overdueCount > 0) message += $"   ⚠️ Quá hạn: {overdueCount} công việc\n";
            if (todayCount > 0) message += $"   📅 Hôm nay: {todayCount} công việc\n";
            if (upcomingCount > 0) message += $"   🔜 Sắp tới (≤3 ngày): {upcomingCount} công việc\n";

            // ✅ ADD: Status breakdown
            var statusBreakdown = tasks.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count());
            message += $"\n📋 **Phân loại theo trạng thái:**\n";
            foreach (var status in statusBreakdown)
            {
                var statusIcon = GetStatusIcon(status.Key);
                var statusText = GetStatusText(status.Key);
                message += $"   {statusIcon} {statusText}: {status.Value} công việc\n";
            }

            // ✅ ADD: Priority breakdown
            var priorityBreakdown = tasks.GroupBy(t => t.Priority).ToDictionary(g => g.Key, g => g.Count());
            message += $"\n🎯 **Phân loại theo độ ưu tiên:**\n";
            foreach (var priority in priorityBreakdown)
            {
                var priorityIcon = GetPriorityIcon(priority.Key);
                var priorityText = GetPriorityText(priority.Key);
                message += $"   {priorityIcon} {priorityText}: {priority.Value} công việc\n";
            }

            // ✅ ENHANCED: Detailed task list with more info
            message += $"\n📋 **Danh sách chi tiết (hiển thị {Math.Min(10, tasks.Count)} đầu tiên):**\n";

            foreach (var task in tasks.Take(10))
            {
                // ✅ Task header with priority and status
                var priorityIcon = GetPriorityIcon(task.Priority);
                var statusIcon = GetStatusIcon(task.Status);
                message += $"\n🔸 **{task.Title}** {priorityIcon}{statusIcon}\n";

                // ✅ Assignee info
                var assigneeName = !string.IsNullOrWhiteSpace(task.AssignedToName) ? task.AssignedToName : "Chưa có tên";
                message += $"   👤 **Người thực hiện:** {assigneeName}\n";
                message += $"   📧 **Email:** {task.AssignedToEmail}\n";

                // ✅ Due date with detailed timing
                if (task.DueDate.HasValue)
                {
                    var dueDate = task.DueDate.Value;
                    var timeSpan = dueDate - now;
                    var daysUntil = timeSpan.Days;
                    var hoursUntil = timeSpan.TotalHours;

                    var dueDateText = dueDate.ToString("dd/MM/yyyy HH:mm");

                    if (dueDate < now)
                    {
                        var daysOverdue = (now - dueDate).Days;
                        var hoursOverdue = (now - dueDate).TotalHours;

                        if (daysOverdue > 0)
                        {
                            message += $"   ⚠️ **Hạn chót:** {dueDateText} (QUÁ HẠN {daysOverdue} ngày)\n";
                        }
                        else
                        {
                            message += $"   ⚠️ **Hạn chót:** {dueDateText} (QUÁ HẠN {hoursOverdue:F1} giờ)\n";
                        }
                    }
                    else if (dueDate.Date == now.Date)
                    {
                        message += $"   📅 **Hạn chót:** {dueDateText} (HÔM NAY - còn {hoursUntil:F1} giờ)\n";
                    }
                    else if (daysUntil <= 3)
                    {
                        message += $"   🔜 **Hạn chót:** {dueDateText} (còn {daysUntil} ngày)\n";
                    }
                    else
                    {
                        message += $"   📅 **Hạn chót:** {dueDateText} (còn {daysUntil} ngày)\n";
                    }
                }
                else
                {
                    message += $"   📅 **Hạn chót:** Chưa có hạn\n";
                }

                // ✅ Additional info
                var statusText = GetStatusText(task.Status);
                var priorityText = GetPriorityText(task.Priority);
                message += $"   📊 **Trạng thái:** {statusText} | **Ưu tiên:** {priorityText}\n";

                // ✅ Program info (if available)
                if (!string.IsNullOrWhiteSpace(task.ProgramId))
                {
                    message += $"   🏷️ **Chương trình:** {task.ProgramId}\n";
                }

                // ✅ Description preview
                if (!string.IsNullOrWhiteSpace(task.Description))
                {
                    var descPreview = task.Description.Length > 50 ?
                        task.Description.Substring(0, 50) + "..." :
                        task.Description;
                    message += $"   📝 **Mô tả:** {descPreview}\n";
                }
            }

            if (tasks.Count > 10)
            {
                message += $"\n... **và {tasks.Count - 10} công việc khác**\n";
            }

            // ✅ ADD: Email notification details
            message += $"\n📧 **Chi tiết email nhắc nhở:**\n";
            message += $"   ✉️ Gửi tới: {tasks.Select(t => t.AssignedToEmail).Distinct().Count()} địa chỉ email khác nhau\n";
            message += $"   📝 Nội dung: Thông tin chi tiết về công việc và hạn chót\n";
            message += $"   ⏰ Thời gian gửi: Ngay bây giờ ({now:dd/MM/yyyy HH:mm})\n";

            // ✅ ADD: Important notes
            message += $"\n⚠️ **Lưu ý quan trọng:**\n";
            message += $"   • Chỉ gửi cho công việc CHƯA hoàn thành/hủy\n";
            message += $"   • Email sẽ chứa link quay lại hệ thống\n";
            message += $"   • Người được assign sẽ nhận được thông báo chi tiết\n";
            message += $"   • Hệ thống sẽ ghi lại lịch sử gửi nhắc nhở\n";

            message += $"\n❓ **Bạn có muốn tiếp tục gửi {tasks.Count} nhắc nhở không?**";

            return message;
        }

        // ✅ ADD: Helper methods for icons and text
        private string GetStatusIcon(TaskItemStatus status)
        {
            return status switch
            {
                TaskItemStatus.NotStarted => "⭕",
                TaskItemStatus.InProgress => "🔄",
                TaskItemStatus.Completed => "✅",
                TaskItemStatus.Canceled => "❌",
                _ => "❓"
            };
        }

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

        private string GetPriorityIcon(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => "🟢",
                TaskPriority.Medium => "🟡",
                TaskPriority.High => "🟠",
                TaskPriority.Critical => "🔴",
                _ => "⚪"
            };
        }

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

        // ✅ THÊM: Send reminders 
        private async Task<AdminReminderResult> SendRemindersAsync(List<TaskItem> tasks)
        {
            var result = new AdminReminderResult();
            result.TotalTasks = tasks.Count;

            ShowStatus($"🔄 Đang gửi {tasks.Count} nhắc nhở...", "#17a2b8");

            foreach (var task in tasks)
            {
                try
                {
                    Debug.WriteLine($"Sending reminder for: {task.Title} → {task.AssignedToEmail}");

                    var success = await _taskService.SendTaskReminderAsync(task.Id);

                    if (success)
                    {
                        result.SuccessCount++;
                        Debug.WriteLine($"✅ Reminder sent successfully for: {task.Title}");
                    }
                    else
                    {
                        result.FailCount++;
                        result.FailedTasks.Add($"{task.Title} → {task.AssignedToEmail}");
                        Debug.WriteLine($"❌ Failed to send reminder for: {task.Title}");
                    }

                    // Small delay to avoid overwhelming the server
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.FailedTasks.Add($"{task.Title} → {task.AssignedToEmail} (Error: {ex.Message})");
                    Debug.WriteLine($"❌ Exception sending reminder for {task.Title}: {ex.Message}");
                }
            }

            return result;
        }

        // ✅ THÊM: Show reminder results
        private void ShowReminderResults(AdminReminderResult result)
        {
            var message = $"📊 Kết quả gửi nhắc nhở từ Admin Panel:\n\n" +
                         $"✅ Thành công: {result.SuccessCount}/{result.TotalTasks}\n" +
                         $"❌ Thất bại: {result.FailCount}/{result.TotalTasks}\n" +
                         $"📈 Tỷ lệ thành công: {(result.SuccessCount * 100.0 / result.TotalTasks):F1}%";

            if (result.FailCount > 0)
            {
                message += $"\n\n❌ Các công việc gửi thất bại:\n";
                message += string.Join("\n", result.FailedTasks.Take(5));

                if (result.FailedTasks.Count > 5)
                {
                    message += $"\n... và {result.FailedTasks.Count - 5} lỗi khác";
                }
            }

            var statusColor = result.FailCount > 0 ? "#ffc107" : "#28a745";
            var statusText = result.FailCount > 0 ?
                $"⚠️ Hoàn thành với {result.FailCount} lỗi" :
                "✅ Gửi nhắc nhở thành công";

            ShowStatus(statusText, statusColor);

            MessageBox.Show(message, "Kết quả gửi nhắc nhở - Admin Panel",
                MessageBoxButton.OK,
                result.FailCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        // ✅ THÊM: UI Helper methods
        private void SetReminderLoadingState(bool isLoading)
        {
            ReminderButtonBorder.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            ReminderLoadingBorder.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowStatus(string message, string color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
            StatusTextBlock.Visibility = Visibility.Visible;

            // Auto hide after 8 seconds
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(8);
            timer.Tick += (s, e) =>
            {
                StatusTextBlock.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        // ✅ EXISTING METHODS: Keep all existing methods...
        private void AdminTasksView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("/DoanKhoaClient;component/Resources/TaskViewResources.xaml", UriKind.Relative)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải tài nguyên: {ex.Message}");
            }
        }

        private void GoToTasks(object sender, MouseButtonEventArgs e)
        {
            var adminTasksView = new AdminTasksView();
            adminTasksView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminTasksView.Show();
            this.Close();
        }

        private void GoToActivities(object sender, MouseButtonEventArgs e)
        {
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminActivitiesView.Show();
            this.Close();
        }

        private void GoToChat(object sender, MouseButtonEventArgs e)
        {
            var chatView = new UserChatView();
            chatView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            chatView.Show();
            this.Close();
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_Task_Background);
        }

        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e) { }

        private void SidebarChatButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new UserChatView();
            win.Show();
            this.Close();
        }

        private void SidebarActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ActivitiesView();
            win.Show();
            this.Close();
        }

        private void SidebarMembersButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new MembersView();
            win.Show();
            this.Close();
        }

        private void SidebarTasksButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new TasksView();
            win.Show();
            this.Close();
        }

        private void SidebarAdminButton_Click(object sender, RoutedEventArgs e)
        {
            isAdminSubmenuOpen = !isAdminSubmenuOpen;
            AdminSubmenu.Visibility = isAdminSubmenuOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AdminTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var adminTaskView = new AdminTasksView();
            adminTaskView.Show();
            this.Close();
        }

        private void AdminMembersButton_Click(object sender, RoutedEventArgs e)
        {
            var adminMembersView = new AdminMembersView();
            adminMembersView.Show();
            this.Close();
        }

        private void AdminChatButton_Click(object sender, RoutedEventArgs e)
        {
            var adminChatView = new AdminChatView();
            adminChatView.Show();
            this.Close();
        }

        private void AdminActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.Show();
            this.Close();
        }

        private void CreateSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CreateSessionCommand.CanExecute(null))
            {
                _viewModel.CreateSessionCommand.Execute(null);
            }
        }

        private void EditSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.EditSessionCommand.CanExecute(_viewModel.SelectedSession))
            {
                _viewModel.EditSessionCommand.Execute(_viewModel.SelectedSession);
            }
        }

        private void SessionsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedSession != null &&
                _viewModel.ViewSessionDetailsCommand.CanExecute(_viewModel.SelectedSession))
            {
                _viewModel.ViewSessionDetailsCommand.Execute(_viewModel.SelectedSession);
            }
        }

        private void SessionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // ✅ THÊM: Admin Result model
    public class AdminReminderResult
    {
        public int TotalTasks { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> FailedTasks { get; set; } = new List<string>();
    }
}