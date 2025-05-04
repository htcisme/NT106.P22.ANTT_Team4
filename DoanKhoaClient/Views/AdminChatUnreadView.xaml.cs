using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminChatUnreadView : Window
    {
        public AdminChatUnreadView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_ChatUnread_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_ChatUnread_Background);
        }
    }
}

