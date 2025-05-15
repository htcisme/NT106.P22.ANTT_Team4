using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Helpers;

namespace DoanKhoaClient.Views
{
    public partial class AdminChatView : Window
    {
        public AdminChatView()
        {
            InitializeComponent();

            // Kiểm tra quyền admin
            AccessControl.CheckAdminAccess(this);
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(AdminChat_Background);
        }
    }
}