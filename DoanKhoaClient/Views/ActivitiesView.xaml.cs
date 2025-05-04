using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class ActivitiesView : Window
    {
        public ActivitiesView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Activities_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Activities_Background);
        }
    }
}

