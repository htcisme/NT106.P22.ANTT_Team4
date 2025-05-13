using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class TasksView : Window
    {
        public TasksView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Task_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Task_Background);
        }




    }
}

