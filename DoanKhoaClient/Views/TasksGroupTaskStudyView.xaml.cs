using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskStudyView : Window
    {
        public TasksGroupTaskStudyView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Study_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Study_Background);
        }
    }
}

