using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.Views
{
    public partial class AdminMembersView : Window
    {
        public AdminMembersView()
        {
            InitializeComponent();

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_Members_Background);

            // Kiểm tra quyền truy cập
            AccessControl.CheckAdminAccess(this);

            // Kiểm tra kích thước cửa sổ
            this.SizeChanged += (sender, e) =>
            {
                if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
                {
                    this.WindowState = WindowState.Normal;
                }
            };
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

        private void GoToDashboard(object sender, MouseButtonEventArgs e)
        {
            var dashboardView = new AdminDashboardView();
            dashboardView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dashboardView.Show();
            this.Close();
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