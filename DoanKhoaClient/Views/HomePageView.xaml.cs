using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class HomePageView : Window
    {
        public HomePageView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(HomePage_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(HomePage_Background);
        }
    }
}

