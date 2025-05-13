using System.Windows;
using DoanKhoaClient.Helpers;
using System.Windows.Input;
namespace DoanKhoaClient.Views
{
    public partial class UserChatView : Window
    {
        public UserChatView()
        {
            InitializeComponent();
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
    }
}