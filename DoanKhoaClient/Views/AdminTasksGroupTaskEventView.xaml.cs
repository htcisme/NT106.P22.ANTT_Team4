using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskEventView : Window
    {
        public AdminTasksGroupTaskEventView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_GroupTask_Event_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Event_Background);
        }
    }
}

