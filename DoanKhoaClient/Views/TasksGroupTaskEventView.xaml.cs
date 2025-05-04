using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskEventView : Window
    {
        public TasksGroupTaskEventView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Event_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Event_Background);
        }
    }
}

