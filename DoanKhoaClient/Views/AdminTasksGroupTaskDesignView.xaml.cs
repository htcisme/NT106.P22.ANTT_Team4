using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskDesignView : Window
    {
        public AdminTasksGroupTaskDesignView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_GroupTask_Design_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Design_Background);
        }
    }
}

