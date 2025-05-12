using System.Windows;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;

namespace DoanKhoaClient.Views
{
    public partial class ActivitiesView : Window
    {
        public ActivitiesView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Activities_Background);
            this.DataContext = new ActivitiesViewModel();
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Activities_Background);
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

        private void LightHomePage_iSearch_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SearchFilterPopup.IsOpen = true;
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            SearchFilterPopup.IsOpen = false;
        }
    }
}

