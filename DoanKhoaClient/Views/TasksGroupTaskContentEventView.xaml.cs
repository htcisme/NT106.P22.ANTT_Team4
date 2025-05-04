using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskContentEventView : Window
    {
        public TasksGroupTaskContentEventView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Content_Event_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Content_Event_Background);
        }
    }
}

