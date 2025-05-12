using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class ActivitiesPostView : Window
    {
        public ActivitiesPostView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(ActivitiesPost_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(ActivitiesPost_Background);
        }
    }
}

