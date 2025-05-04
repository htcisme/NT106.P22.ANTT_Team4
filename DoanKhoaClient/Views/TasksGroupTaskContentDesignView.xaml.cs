using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskContentDesignView : Window
    {
        public TasksGroupTaskContentDesignView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Content_Design_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Content_Design_Background);
        }
    }
}

