using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Helpers;

namespace DoanKhoaClient.Views
{
    public partial class MembersView : Window
    {
        public MembersView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Members_Background);

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
            ThemeManager.ToggleTheme(Members_Background);
        }

        private async void HomeMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToHome(this, Members_Background);
        }

        private async void ChatMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToChat(this, Members_Background);
        }

        private async void ActivitiesMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToActivities(this, Members_Background);
        }

        private void MembersMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Đã ở trang Members, không cần điều hướng
        }

        private async void TasksMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToTasks(this, Members_Background);
        }
    }
}