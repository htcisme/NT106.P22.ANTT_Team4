using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class LightTasksView : Window
    {
        public LightTasksView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(LightTask_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(LightTask_Background);
        }
    }
}

