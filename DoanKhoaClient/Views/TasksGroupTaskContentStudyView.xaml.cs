using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskContentStudyView : Window
    {
        public TasksGroupTaskContentStudyView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Content_Study_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Content_Study_Background);
        }
    }
}

