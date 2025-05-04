using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminChatSpamView : Window
    {
        public AdminChatSpamView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_ChatSpam_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_ChatSpam_Background);
        }
    }
}

