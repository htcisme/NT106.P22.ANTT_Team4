using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskContentStudyView : Window
    {
        public AdminTasksGroupTaskContentStudyView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_GroupTask_Content_Study_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Content_Study_Background);
        }
    }
}

