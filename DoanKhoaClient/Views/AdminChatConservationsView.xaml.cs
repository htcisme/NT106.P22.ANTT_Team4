using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminChatConservationsView : Window
    {
        public AdminChatConservationsView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_ChatConservations_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_ChatConservations_Background);
        }
    }
}

