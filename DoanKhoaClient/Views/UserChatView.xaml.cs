using System.Windows;
using DoanKhoaClient.Helpers;

namespace DoanKhoaClient.Views
{
    public partial class UserChatView : Window
    {
        public UserChatView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(ChatBackGround);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(ChatBackGround);
        }
    }
}