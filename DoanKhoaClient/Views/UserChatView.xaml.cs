using System.Windows;
using DoanKhoaClient.Helpers;

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
    }
}