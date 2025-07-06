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
                Debug.WriteLine("===== CORRECT FLOW: Session → TaskProgram → TaskItem =====");

                // Get all sessions from ViewModel
                if (_viewModel?.Sessions != null && _viewModel.Sessions.Count > 0)
                {
                    Debug.WriteLine($"Found {_viewModel.Sessions.Count} sessions");

                    foreach (var session in _viewModel.Sessions)
                    {
                        try
                        {
                            Debug.WriteLine($"\n--- Processing Session: {session.Name} (Type: {session.Type}) ---");

                            // ✅ STEP 1: Get TaskProgram from Session
                            var taskProgram = await GetTaskProgramFromSessionAsync(session);

                            if (taskProgram != null)
                            {
                                Debug.WriteLine($"✅ Found TaskProgram: {taskProgram.Name} (ID: {taskProgram.Id})");

                                // ✅ STEP 2: Get TaskItems from TaskProgram
                                var taskItems = await GetTaskItemsFromTaskProgramAsync(taskProgram);

                                if (taskItems != null && taskItems.Count > 0)
                                {
                                    Debug.WriteLine($"✅ Found {taskItems.Count} TaskItems in program '{taskProgram.Name}'");
                                    allTaskItems.AddRange(taskItems);

                                    // Debug: Show sample tasks
                                    foreach (var task in taskItems.Take(3))
                                    {
                                        Debug.WriteLine($"   - {task.Title} (Status: {task.Status}, Due: {task.DueDate?.ToString("MM/dd") ?? "NULL"})");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"⚠️ No TaskItems found in program '{taskProgram.Name}'");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"❌ No TaskProgram found for session '{session.Name}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"❌ Error processing session {session.Name}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("⚠️ No sessions found in ViewModel, trying fallback...");

                    // ✅ FALLBACK: Get all TaskPrograms directly
                    var allPrograms = await GetAllTaskProgramsAsync();
                    Debug.WriteLine($"Fallback: Found {allPrograms?.Count ?? 0} TaskPrograms");

                    if (allPrograms != null && allPrograms.Count > 0)
                    {
                        foreach (var program in allPrograms)
                        {
                            try
                            {
                                var taskItems = await GetTaskItemsFromTaskProgramAsync(program);
                                if (taskItems != null && taskItems.Count > 0)
                                {
                                    allTaskItems.AddRange(taskItems);
                                    Debug.WriteLine($"✅ Fallback: Added {taskItems.Count} tasks from '{program.Name}'");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"❌ Fallback error for program {program.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ CRITICAL Error in GetAllTaskItemsFromSessionsAsync: {ex.Message}");
            }

            Debug.WriteLine($"\n===== FINAL RESULT =====");
            Debug.WriteLine($"Total TaskItems collected: {allTaskItems.Count}");
            return allTaskItems;
        }
        private async Task<TaskProgram> GetTaskProgramFromSessionAsync(TaskSession session)
        {
            try
            {
                Debug.WriteLine($"Getting TaskProgram for session: {session.Name}");
                Debug.WriteLine($"Session Type: {session.Type}");
                Debug.WriteLine($"Session TaskProgramId: '{session.TaskProgramId ?? "NULL"}'");

                // ✅ METHOD 1: If session has direct TaskProgram reference
                if (!string.IsNullOrEmpty(session.TaskProgramId))
                {
                    Debug.WriteLine($"Session has TaskProgramId: {session.TaskProgramId}");
                    var taskProgramService = new TaskProgramService();
                    var program = await taskProgramService.GetTaskProgramByIdAsync(session.TaskProgramId);

                    if (program != null)
                    {
                        Debug.WriteLine($"✅ Found TaskProgram by ID: {program.Name}");
                        return program;
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ TaskProgram not found for ID: {session.TaskProgramId}");
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

                        // ✅ UPDATE: Set TaskProgramId for future use
                        session.TaskProgramId = program.Id;

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

                    // ✅ UPDATE: Set TaskProgramId for future use
                    session.TaskProgramId = matchingProgram.Id;

                    return matchingProgram;
                }

                // ✅ METHOD 4: Create default TaskProgram if none found
                Debug.WriteLine("No TaskProgram found, creating default...");
                var defaultProgram = CreateDefaultTaskProgramForSession(session);
                if (defaultProgram != null)
                {
                    Debug.WriteLine($"✅ Created default TaskProgram: {defaultProgram.Name}");
                    session.TaskProgramId = defaultProgram.Id;
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

            var message = $"🔔 Sẽ gửi nhắc nhở cho {tasks.Count} công việc:\n\n";

            if (overdueCount > 0)
            {
                message += $"⚠️ Quá hạn: {overdueCount} công việc\n";
            }

            if (upcomingCount > 0)
            {
                message += $"📅 Sắp đến hạn: {upcomingCount} công việc\n";
            }

            message += "\n📋 Danh sách:\n";

            foreach (var task in tasks.Take(8))
            {
                // ✅ FIX: Handle nullable DueDate
                if (task.DueDate.HasValue)
                {
                    var status = task.DueDate.Value < now ? "⚠️ QUÁ HẠN" : "📅 SẮP TỚI";
                    var dueDate = task.DueDate.Value.ToString("dd/MM");
                    message += $"• {task.Title} → {task.AssignedToEmail} ({status} - {dueDate})\n";
                }
                else
                {
                    message += $"• {task.Title} → {task.AssignedToEmail} (📅 Chưa có hạn)\n";
                }
            }

            if (tasks.Count > 8)
            {
                message += $"... và {tasks.Count - 8} công việc khác\n";
            }

            message += "\n❌ Lưu ý: Công việc đã hoàn thành hoặc bị hủy sẽ KHÔNG được gửi nhắc nhở.";
            message += "\n\nBạn có muốn tiếp tục gửi nhắc nhở không?";

            return message;
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