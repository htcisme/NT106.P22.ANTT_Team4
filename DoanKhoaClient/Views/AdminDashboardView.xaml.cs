using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using System;
using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class AdminDashboardView : Window
    {
        public AdminDashboardView()
        {
            InitializeComponent();

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_Dashboard_Background);

            // Kiểm tra người dùng có phải là admin không
            AccessControl.CheckAdminAccess(this);

            // Hiển thị tên admin
            LoadAdminInfo();

            // Check window size
            this.SizeChanged += (sender, e) =>
            {
                if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
                {
                    this.WindowState = WindowState.Normal;
                }
            };
        }

        private void LoadAdminInfo()
        {
            if (Application.Current.Properties.Contains("CurrentUser") &&
                Application.Current.Properties["CurrentUser"] is User currentUser)
            {
                AdminNameText.Text = $"Xin chào, {currentUser.DisplayName}";
            }
        }

        private void TasksButton_Click(object sender, RoutedEventArgs e)
        {
            var adminTasksView = new AdminTasksView();
            adminTasksView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminTasksView.Show();
            this.Close();
        }

        private void ActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminActivitiesView.Show();
            this.Close();
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            var adminChatView = new UserChatView(); // Assuming the chat view is the same
            adminChatView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminChatView.Show();
            this.Close();
        }

        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            var adminMembersView = new AdminMembersView();
            adminMembersView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminMembersView.Show();
            this.Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng cài đặt sẽ được phát triển trong phiên bản sau.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Xác nhận đăng xuất
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Xóa thông tin đăng nhập hiện tại
                if (Application.Current.Properties.Contains("CurrentUser"))
                {
                    Application.Current.Properties.Remove("CurrentUser");
                }

                // Chuyển về trang đăng nhập
                var loginView = new LoginView();
                loginView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                loginView.Show();
                this.Close();
            }
        }
    }
}