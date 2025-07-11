using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskDetailView : Window
    {
        private readonly TaskProgram _program; // THAY ĐỔI: từ GroupContent thành TaskProgram
        private readonly TaskService _taskService;
        private List<TaskItem> _tasks;
        private int _currentPage = 1;
        private const int TasksPerPage = 5;

        public TasksGroupTaskDetailView(TaskProgram program) // THAY ĐỔI: param type
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Detail_Background);
            _program = program;
            _taskService = new TaskService();
            LoadProgramTasks(); // THAY ĐỔI: method name
        }

        private async void LoadProgramTasks() // THAY ĐỔI: method name
        {
            try
            {
                // Cập nhật thông tin TaskProgram
                if (_program != null)
                {
                    GroupContent_lbName.Content = _program.Name ?? "Không có tên";
                    GroupContent_lbDescription.Content = _program.Description ?? "Không có mô tả";
                }

                // THAY ĐỔI: Lấy TaskItems theo Program ID
                _tasks = await _taskService.GetTaskItemsByProgramAsync(_program?.Id ?? "");

                UpdateTaskDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách công việc: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateTaskDisplay()
        {
            ClearDynamicTasks();

            if (_tasks?.Any() != true)
            {
                ShowNoTasksMessage();
                return;
            }

            var totalPages = (int)Math.Ceiling((double)_tasks.Count / TasksPerPage);
            var startIndex = (_currentPage - 1) * TasksPerPage;
            var tasksToShow = _tasks.Skip(startIndex).Take(TasksPerPage).ToList();

            foreach (var task in tasksToShow)
            {
                CreateTaskCard(task);
            }

            UpdateNavigationButtons(totalPages);
        }

        private void CreateTaskCard(TaskItem task)
        {
            var taskCard = new Border
            {
                Style = (Style)this.FindResource("TaskCardStyle"),
                Width = 1190,
                Height = 160,
            };

            var grid = new Grid
            {
                Margin = new Thickness(20)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Task Info Panel
            var infoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(infoPanel, 0);

            // Task Title
            var titleLabel = new Label
            {
                Content = task.Title ?? "Không có tiêu đề",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(4, 35, 84)),
                Padding = new Thickness(0, 0, 0, 5)
            };

            // Task Description
            var descriptionLabel = new Label
            {
                Content = task.Description ?? "Không có mô tả",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Thickness(0, 0, 0, 5),
                MaxWidth = 800
            };

            // Assignee and Deadline
            var detailsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var assigneeLabel = new Label
            {
                Content = $"Người thực hiện: {task.AssignedToName ?? "Chưa phân công"}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(4, 35, 84)),
                Padding = new Thickness(0, 0, 20, 0)
            };

            var deadlineLabel = new Label
            {
                Content = $"Hạn chót: {task.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác định"}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(4, 35, 84)),
                Padding = new Thickness(0)
            };

            detailsPanel.Children.Add(assigneeLabel);
            detailsPanel.Children.Add(deadlineLabel);

            // Status and Priority
            var statusPriorityPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var statusLabel = new Label
            {
                Content = GetStatusText(task.Status),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = GetStatusColor(task.Status),
                Padding = new Thickness(0, 0, 20, 0)
            };

            var priorityLabel = new Label
            {
                Content = $"Độ ưu tiên: {GetPriorityText(task.Priority)}",
                FontSize = 12,
                Foreground = GetPriorityColor(task.Priority),
                Padding = new Thickness(0)
            };

            statusPriorityPanel.Children.Add(statusLabel);
            statusPriorityPanel.Children.Add(priorityLabel);

            infoPanel.Children.Add(titleLabel);
            infoPanel.Children.Add(descriptionLabel);
            infoPanel.Children.Add(detailsPanel);
            infoPanel.Children.Add(statusPriorityPanel);

            // Action Buttons Panel
            var buttonPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(buttonPanel, 1);

            // Complete Button
            if (task.Status != TaskItemStatus.Completed)
            {
                var completeButton = new Button
                {
                    Content = "Hoàn thành",
                    Width = 100,
                    Height = 35,
                    Margin = new Thickness(0, 5, 0, 5),
                    Style = (Style)this.FindResource("ButtonStyle")
                };
                completeButton.Click += (s, e) => CompleteTask(task);
                buttonPanel.Children.Add(completeButton);
            }

            // Detail Button
            var detailButton = new Button
            {
                Content = "Chi tiết",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 5, 0, 5),
                Style = (Style)this.FindResource("ButtonStyle"),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            detailButton.Click += (s, e) => ShowTaskDetail(task);
            buttonPanel.Children.Add(detailButton);

            grid.Children.Add(infoPanel);
            grid.Children.Add(buttonPanel);
            taskCard.Child = grid;

            DynamicTasksPanel.Children.Add(taskCard);
        }

        private string GetStatusText(TaskItemStatus status)
        {
            return status switch
            {
                TaskItemStatus.NotStarted => "Chưa bắt đầu",
                TaskItemStatus.InProgress => "Đang thực hiện",
                TaskItemStatus.Completed => "Đã hoàn thành",
                TaskItemStatus.Canceled => "Đã hủy",
                TaskItemStatus.Delayed => "Tạm hoãn",
                TaskItemStatus.Pending => "Chờ xử lý",
                _ => "Không xác định"
            };
        }

        private SolidColorBrush GetStatusColor(TaskItemStatus status)
        {
            return status switch
            {
                TaskItemStatus.NotStarted => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                TaskItemStatus.InProgress => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                TaskItemStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                TaskItemStatus.Canceled => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                TaskItemStatus.Delayed => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                TaskItemStatus.Pending => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
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
                _ => "Không xác định"
            };
        }

        private SolidColorBrush GetPriorityColor(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                TaskPriority.Medium => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                TaskPriority.High => new SolidColorBrush(Color.FromRgb(255, 102, 0)),
                TaskPriority.Critical => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };
        }

        private async void CompleteTask(TaskItem task)
        {
            try
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn đánh dấu công việc '{task.Title}' là hoàn thành?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _taskService.CompleteTaskItemAsync(task.Id);
                    LoadProgramTasks(); // SỬA LẠI: từ LoadGroupContentTasks thành LoadProgramTasks
                    MessageBox.Show("Công việc đã được đánh dấu hoàn thành!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật trạng thái: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowTaskDetail(TaskItem task)
        {
            try
            {
                var detailWindow = new Window
                {
                    Title = $"Chi tiết: {task.Title}",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(20)
                };

                var stackPanel = new StackPanel();

                // Add detailed task information
                stackPanel.Children.Add(CreateDetailLabel("Tiêu đề:", task.Title ?? "Không có tiêu đề"));
                stackPanel.Children.Add(CreateDetailLabel("Mô tả:", task.Description ?? "Không có mô tả"));
                stackPanel.Children.Add(CreateDetailLabel("Người thực hiện:", task.AssignedToName ?? "Chưa phân công"));
                stackPanel.Children.Add(CreateDetailLabel("Hạn chót:", task.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác định"));
                stackPanel.Children.Add(CreateDetailLabel("Trạng thái:", GetStatusText(task.Status), GetStatusColor(task.Status)));
                stackPanel.Children.Add(CreateDetailLabel("Độ ưu tiên:", GetPriorityText(task.Priority), GetPriorityColor(task.Priority)));

                scrollViewer.Content = stackPanel;
                detailWindow.Content = scrollViewer;
                detailWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi hiển thị chi tiết: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private StackPanel CreateDetailLabel(string label, string value, SolidColorBrush valueColor = null)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };

            panel.Children.Add(new Label
            {
                Content = label,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Padding = new Thickness(0, 0, 0, 2)
            });

            panel.Children.Add(new Label
            {
                Content = value,
                Foreground = valueColor ?? Brushes.Black,
                Padding = new Thickness(0)
            });

            return panel;
        }

        private void ShowNoTasksMessage()
        {
            var noTasksLabel = new Label
            {
                Content = "Chưa có công việc nào trong nhóm này",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };

            DynamicTasksPanel.Children.Add(noTasksLabel);
        }

        private void UpdateNavigationButtons(int totalPages)
        {
            PageInfoLabel.Content = $"Trang {_currentPage} / {totalPages}";
            PreviousPageButton.Visibility = _currentPage > 1 ? Visibility.Visible : Visibility.Collapsed;
            NextPageButton.Visibility = _currentPage < totalPages ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateTaskDisplay();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            var totalPages = (int)Math.Ceiling((double)_tasks.Count / TasksPerPage);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdateTaskDisplay();
            }
        }

        private void ClearDynamicTasks()
        {
            DynamicTasksPanel.Children.Clear();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}