using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.Views
{
    public partial class AdminTaskGroupTaskEventView : Window
    {
        private AdminTaskProgramsViewModel _viewModel;

        public AdminTaskGroupTaskEventView(TaskSession session)
        {
            InitializeComponent();
            
            _viewModel = new AdminTaskProgramsViewModel(session, ProgramType.Event);
            DataContext = _viewModel;

            ThemeManager.ApplyTheme(Admin_GroupTask_Event_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Event_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}