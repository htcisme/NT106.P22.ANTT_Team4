using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskContentDesignView : Window
    {
        public AdminTasksGroupTaskContentDesignView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_GroupTask_Content_Design_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Content_Design_Background);
        }
    }
}

