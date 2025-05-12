using System.Windows;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class ActivitiesPostView : Window
    {
        public ActivitiesPostView(Activity activity)
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(ActivitiesPost_Background);
            this.DataContext = activity;
        }

        public ActivitiesPostView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(ActivitiesPost_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(ActivitiesPost_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Đóng cửa sổ hiện tại
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

        private void SidebarActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            // Đang ở trang này, có thể không cần xử lý hoặc chỉ cần return
        }

        private void SidebarMembersButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new HomePageView();
            win.Show();
            this.Close();
        }

        private void SidebarTasksButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new TasksView();
            win.Show();
            this.Close();
        }
    }
}

