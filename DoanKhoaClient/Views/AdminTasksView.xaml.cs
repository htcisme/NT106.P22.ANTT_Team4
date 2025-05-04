using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminTasksView : Window
    {
        public AdminTasksView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_Task_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_Task_Background);
        }
    }
}

