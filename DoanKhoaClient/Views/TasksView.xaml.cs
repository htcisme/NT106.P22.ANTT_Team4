using System.Windows;
using DoanKhoaClient.Helpers;

namespace DoanKhoaClient.Views
{
    public partial class TasksView : Window
    {
        public TasksView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Task_Background);
                this.SizeChanged += (sender, e) =>
    {
        if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
        {
            this.WindowState = WindowState.Normal;
        }
    };
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Task_Background);
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
            var win = new ActivitiesView();
            win.Show();
            this.Close();
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


        private async void HomeMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToHome(this, Task_Background);
        }

        private async void ChatMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToChat(this, Task_Background);
        }

        private async void ActivitiesMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToActivities(this, Task_Background);
        }

        private async void MembersMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToMembers(this, Task_Background);
        }

        private void TasksMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Đã ở trang Tasks, không cần điều hướng
        }

        // Thêm các phương thức FilterDropdownButton_Unchecked và FilterPopupBorder_Loaded đã có trong code
    }
}