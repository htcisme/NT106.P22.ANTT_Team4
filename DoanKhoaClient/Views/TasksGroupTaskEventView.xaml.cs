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
    public partial class TasksGroupTaskEventView : Window
    {
        private readonly TaskService _taskService;
        private List<TaskSession> _sessions;

        public TasksGroupTaskEventView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Event_Background);
            _taskService = new TaskService();
            LoadEventSessions();
        }

        private async void LoadEventSessions()
        {
            try
            {
                // Lấy tất cả sessions của loại Event
                var allSessions = await _taskService.GetTaskSessionsAsync();
                _sessions = allSessions?.Where(s => s.Type == TaskSessionType.Event).ToList() ?? new List<TaskSession>();

                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshSessions()
        {
            LoadEventSessions();
        }

        private void UpdateUI()
        {
            if (_sessions?.Any() == true)
            {
                UpdateManagerDisplay("Huỳnh Ngọc Ngân Tuyền");
                CreateSessionLabels();
            }
            else
            {
                UpdateManagerDisplay("Huỳnh Ngọc Ngân Tuyền");
                ClearDynamicControls();
            }
        }

        private void UpdateManagerDisplay(string managerName)
        {
            try
            {
                var managerLabel = this.FindName("GroupTask_Event_lbManagerEventTeam") as Label;
                if (managerLabel != null)
                {
                    managerLabel.Content = managerName;
                }
                else
                {
                    CreateManagerLabel(managerName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating manager display: {ex.Message}");
                CreateManagerLabel(managerName);
            }
        }

        private void CreateManagerLabel(string managerName)
        {
            try
            {
                // Xóa label cũ nếu có
                var oldLabel = GroupTask_Event_Background.Children
                    .OfType<Label>()
                    .FirstOrDefault(l => l.Name == "DynamicManagerLabel");
                if (oldLabel != null)
                {
                    GroupTask_Event_Background.Children.Remove(oldLabel);
                }

                var managerLabel = new Label
                {
                    Name = "DynamicManagerLabel",
                    Content = managerName,
                    Width = 234,
                    Height = 30,
                    Margin = new Thickness(462, 199, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    FontSize = 20,
                    Foreground = new SolidColorBrush(Color.FromRgb(4, 35, 84))
                };

                GroupTask_Event_Background.Children.Add(managerLabel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating manager label: {ex.Message}");
            }
        }

        private void CreateSessionLabels()
        {
            ClearDynamicControls();

            // THAY ĐỔI: Chỉ lấy tối đa 10 sessions đầu tiên
            var displaySessions = _sessions.Take(10).ToList();

            for (int i = 0; i < displaySessions.Count; i++)
            {
                var session = displaySessions[i];
                CreateSessionUI(session, i);
            }
        }

        private void CreateSessionUI(TaskSession session, int index)
        {
            var sessionBorder = new Border
            {
                Style = (Style)this.FindResource("SessionCardStyle"),
                Width = 1000,
                Height = 120,
                Margin = new Thickness(10, 10, 10, 10),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            sessionBorder.MouseDown += (s, e) => OpenSessionContent(session);

            var grid = new Grid
            {
                Margin = new Thickness(25)
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon Container
            var iconBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(4, 35, 84)),
                CornerRadius = new CornerRadius(10),
                Width = 70,
                Height = 70,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(iconBorder, 0);

            var icon = new Image
            {
                Source = new BitmapImage(new Uri("/Views/Images/active-activities.png", UriKind.Relative)),
                Width = 35,
                Height = 35,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = icon;

            // Info Panel
            var infoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(25, 0, 0, 0)
            };
            Grid.SetColumn(infoPanel, 1);

            var sessionNameLabel = new Label
            {
                Content = session.Name,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(4, 35, 84)),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var sessionInfoLabel = new Label
            {
                Content = $"Tạo: {session.CreatedAt:dd/MM/yyyy} | Manager: {session.ManagerName}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // SỬA LẠI: Đơn giản hóa task count - không cần đếm thực tế
            var taskCountLabel = new Label
            {
                Content = "Sự kiện truyền thông", // Text cố định
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            infoPanel.Children.Add(sessionNameLabel);
            infoPanel.Children.Add(sessionInfoLabel);
            infoPanel.Children.Add(taskCountLabel);

            // Arrow Icon
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

            grid.Children.Add(iconBorder);
            grid.Children.Add(infoPanel);
            grid.Children.Add(arrow);
            sessionBorder.Child = grid;

            var contentPanel = this.FindName("DynamicContentPanel") as StackPanel;
            contentPanel?.Children.Add(sessionBorder);
        }

        // THÊM: Method OpenSessionContent
        private void OpenSessionContent(TaskSession session)
        {
            try
            {
                var contentView = new TasksGroupTaskContentEventView(session);
                contentView.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở chi tiết phiên sự kiện: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDynamicControls()
        {
            var contentPanel = this.FindName("DynamicContentPanel") as StackPanel;
            contentPanel?.Children.Clear();
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Event_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}