using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskDesignView : Window
    {
        public TasksGroupTaskDesignView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Design_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Design_Background);
        }
    }
}

