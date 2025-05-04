using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminActivitiesPostView : Window
    {
        public AdminActivitiesPostView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_ActivitiesPost_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_ActivitiesPost_Background);
        }
    }
}

