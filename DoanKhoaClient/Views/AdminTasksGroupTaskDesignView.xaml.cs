using System.Windows;
using DoanKhoaClient.Helpers;


namespace DoanKhoaClient.Views
{
    public partial class AdminLightTasksGroupTaskDesignView : Window
    {
        public AdminLightTasksGroupTaskDesignView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Admin_LightGroupTask_Design_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_LightGroupTask_Design_Background);
        }
    }
}

