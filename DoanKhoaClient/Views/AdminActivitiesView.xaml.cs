using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminActivitiesView : Window
    {
        public AdminActivitiesView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_Activities_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_Activities_Background);
        }
    }
}

