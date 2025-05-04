using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskContentEventView : Window
    {
        public AdminTasksGroupTaskContentEventView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_GroupTask_Content_Event_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Content_Event_Background);
        }
    }
}

