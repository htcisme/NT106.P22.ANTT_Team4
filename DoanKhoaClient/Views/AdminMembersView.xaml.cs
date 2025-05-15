using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Extensions;
namespace DoanKhoaClient.Views
{
    public partial class AdminMembersView : Window
    {
        private bool _isDarkMode;
        private bool isAdminSubmenuOpen; // Add this field declaration
        public AdminMembersView()
        {
            InitializeComponent();

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_Members_Background);

            // Kiểm tra quyền truy cập
            AccessControl.CheckAdminAccess(this);

            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }

            Admin_Members_iUsers.SetupAsUserAvatar();

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
            ThemeManager.ToggleTheme(Admin_Members_Background);
        }


        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {

        }

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
            // Toggle hiển thị submenu admin
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


    }
}