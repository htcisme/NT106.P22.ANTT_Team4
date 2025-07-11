using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskContentDesignView : Window
    {
        private readonly TaskSession _session;
        private readonly TaskService _taskService;
        private List<TaskProgram> _programs; // THAY ĐỔI: Hiển thị TaskProgram thay vì TaskItem
        private int _currentPage = 1;
        private const int ProgramsPerPage = 5; // THAY ĐỔI: từ TasksPerPage thành ProgramsPerPage
        private bool isAdminSubmenuOpen = false;

        public TasksGroupTaskContentDesignView(TaskSession session)
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Content_Design_Background);
            _session = session;
            _taskService = new TaskService();
            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }
            LoadSessionPrograms(); // THAY ĐỔI: method name
        }

        private async void LoadSessionPrograms() // THAY ĐỔI: method name và logic
        {
            try
            {
                // Cập nhật thông tin session
                if (_session != null)
                {
                    GroupTask_Design_lbManagerDesignTeam.Content = _session.ManagerName ?? "Hoàng Bảo Phước";
                    GroupTask_Design_lbSessionName.Content = _session.Name?.ToUpper() ?? "PHIÊN LÀM VIỆC THIẾT KẾ";
                }

                // THAY ĐỔI: Lấy TaskPrograms theo sessionId và filter theo Type Design
                var allPrograms = await _taskService.GetTaskProgramsAsync(_session?.Id ?? "");
                _programs = allPrograms?.Where(p => p.Type == ProgramType.Design).ToList() ?? new List<TaskProgram>();

                UpdateProgramDisplay(); // THAY ĐỔI: method name
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProgramDisplay() // THAY ĐỔI: method name và logic
        {
            ClearDynamicPrograms(); // THAY ĐỔI: method name

            if (_programs?.Any() != true)
            {
                ShowNoProgramsMessage(); // THAY ĐỔI: method name
                return;
            }

            var totalPages = (int)Math.Ceiling((double)_programs.Count / ProgramsPerPage);
            var startIndex = (_currentPage - 1) * ProgramsPerPage;
            var programsToShow = _programs.Skip(startIndex).Take(ProgramsPerPage).ToList();

            foreach (var program in programsToShow)
            {
                CreateProgramCard(program); // THAY ĐỔI: method name và param
            }

            UpdateNavigationButtons(totalPages);
        }

        private void CreateProgramCard(TaskProgram program) // THAY ĐỔI: toàn bộ method
        {
            var programCard = new Border
            {
                Style = (Style)this.FindResource("TaskCardStyle"),
                Width = 940,
                Height = 130,
            };

            var grid = new Grid
            {
                Margin = new Thickness(20)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon Container
            var iconBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(244, 248, 255)),
                CornerRadius = new CornerRadius(10),
                Width = 60,
                Height = 60,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(iconBorder, 0);

            try
            {
                var icon = new Image
                {
                    Source = new BitmapImage(new Uri("/Views/Images/tasks.png", UriKind.Relative)),
                    Width = 30,
                    Height = 30,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBorder.Child = icon;
            }
            catch
            {
                var fallbackIcon = new Label
                {
                    Content = "🎨", // THAY ĐỔI: icon cho design program
                    FontSize = 24,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(0)
                };
                iconBorder.Child = fallbackIcon;
            }

            // Info Panel
            var infoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(20, 0, 0, 0)
            };
            Grid.SetColumn(infoPanel, 1);

            var programNameLabel = new Label
            {
                Content = program.Name ?? "Không có tên",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(4, 35, 84)),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var programDescLabel = new Label
            {
                Content = program.Description ?? "Không có mô tả",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var dateInfoLabel = new Label
            {
                Content = $"Thời gian: {program.StartDate:dd/MM/yyyy} - {program.EndDate:dd/MM/yyyy}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var statusInfoLabel = new Label
            {
                Content = $"Trạng thái: {GetProgramStatusText(program.Status)} | Người thực hiện: {program.ExecutorName ?? "Chưa phân công"}",
                FontSize = 12,
                Foreground = GetProgramStatusColor(program.Status),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            infoPanel.Children.Add(programNameLabel);
            infoPanel.Children.Add(programDescLabel);
            infoPanel.Children.Add(dateInfoLabel);
            infoPanel.Children.Add(statusInfoLabel);

            // Arrow Icon
            try
            {
                var arrow = new Image
                {
                    Source = new BitmapImage(new Uri("/Views/Images/-down-list.png", UriKind.Relative)),
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 10, 0),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(-90)
                };
                Grid.SetColumn(arrow, 2);
                grid.Children.Add(arrow);
            }
            catch
            {
                var arrowLabel = new Label
                {
                    Content = "▶",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromRgb(4, 35, 84)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(arrowLabel, 2);
                grid.Children.Add(arrowLabel);
            }

            grid.Children.Add(iconBorder);
            grid.Children.Add(infoPanel);
            programCard.Child = grid;

            // THAY ĐỔI: Click event để mở TasksGroupTaskDetailView với TaskProgram
            programCard.MouseDown += (s, e) => OpenProgramDetail(program);

            var tasksPanel = this.FindName("DynamicTasksPanel") as StackPanel;
            tasksPanel?.Children.Add(programCard);
        }

        private void OpenProgramDetail(TaskProgram program) // THAY ĐỔI: method mới với TaskProgram
        {
            try
            {
                var detailView = new TasksGroupTaskDetailView(program);
                detailView.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở chi tiết chương trình: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // THÊM: Helper methods cho Program Status
        private string GetProgramStatusText(ProgramStatus status)
        {
            return status switch
            {
                ProgramStatus.NotStarted => "Chưa bắt đầu",
                ProgramStatus.InProgress => "Đang thực hiện",
                ProgramStatus.Completed => "Đã hoàn thành",
                ProgramStatus.Canceled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private SolidColorBrush GetProgramStatusColor(ProgramStatus status)
        {
            return status switch
            {
                ProgramStatus.NotStarted => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                ProgramStatus.InProgress => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                ProgramStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                ProgramStatus.Canceled => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };
        }

        private void ShowNoProgramsMessage() // THAY ĐỔI: method name và message
        {
            var noProgramsLabel = new Label
            {
                Content = "Chưa có chương trình thiết kế nào trong phiên này",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };

            var tasksPanel = this.FindName("DynamicTasksPanel") as StackPanel;
            tasksPanel?.Children.Add(noProgramsLabel);
        }

        private void UpdateNavigationButtons(int totalPages)
        {
            var pageInfoLabel = this.FindName("PageInfoLabel") as Label;
            if (pageInfoLabel != null)
            {
                pageInfoLabel.Content = $"Trang {_currentPage} / {totalPages}";
            }

            var previousButton = this.FindName("PreviousPageButton") as Button;
            var nextButton = this.FindName("NextPageButton") as Button;

            if (previousButton != null)
                previousButton.Visibility = _currentPage > 1 ? Visibility.Visible : Visibility.Collapsed;

            if (nextButton != null)
                nextButton.Visibility = _currentPage < totalPages ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateProgramDisplay(); // THAY ĐỔI: method name
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            var totalPages = (int)Math.Ceiling((double)_programs.Count / ProgramsPerPage); // THAY ĐỔI: variable name
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdateProgramDisplay(); // THAY ĐỔI: method name
            }
        }

        private void ClearDynamicPrograms() // THAY ĐỔI: method name
        {
            var tasksPanel = this.FindName("DynamicTasksPanel") as StackPanel;
            tasksPanel?.Children.Clear();
        }

        public void RefreshContent()
        {
            LoadSessionPrograms(); // THAY ĐỔI: method name
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Content_Design_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new HomePageView();
            win.Show();
            this.Close();
        }
        private void SidebarChatButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new UserChatView();
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

        private void SidebarActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ActivitiesView();
            win.Show();
            this.Close();
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
        private void SidebarAdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle hiển thị submenu admin
            isAdminSubmenuOpen = !isAdminSubmenuOpen;
            AdminSubmenu.Visibility = isAdminSubmenuOpen ? Visibility.Visible : Visibility.Collapsed;
        }
        private void AdminActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.Show();
            this.Close();
        }



    }
}