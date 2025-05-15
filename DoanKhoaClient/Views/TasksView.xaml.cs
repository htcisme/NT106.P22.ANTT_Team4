using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Extensions;
namespace DoanKhoaClient.Views
{
    public partial class TasksView : Window
    {
        private bool isAdminSubmenuOpen = false;
        public TasksView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Task_Background);
            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }
            this.SizeChanged += (sender, e) =>
{
    if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
    {
        this.WindowState = WindowState.Normal;
    }
    Task_iUsers.SetupAsUserAvatar();
};
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Task_Background);
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
        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {

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
        // Thêm các phương thức FilterDropdownButton_Unchecked và FilterPopupBorder_Loaded đã có trong code
    }
}