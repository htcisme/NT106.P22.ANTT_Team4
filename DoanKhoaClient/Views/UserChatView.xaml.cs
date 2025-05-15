using System.Windows;
using DoanKhoaClient.Helpers;
using System.Windows.Input;
namespace DoanKhoaClient.Views
{
    public partial class UserChatView : Window
    {
        private bool isAdminSubmenuOpen = false;
        public UserChatView()
        {
            InitializeComponent();
            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }
            ThemeManager.ApplyTheme(Chat_Background);
            this.SizeChanged += (sender, e) =>
{
    if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
    {
        this.WindowState = WindowState.Normal;
    }
};
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Chat_Background);
        }
        private async void ChatMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }
        private async void HomeMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToHome(this, Chat_Background);
        }


        private async void ActivitiesMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToActivities(this, Chat_Background);
        }

        private async void MembersMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToMembers(this, Chat_Background);
        }

        private async void TasksMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToTasks(this, Chat_Background);
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