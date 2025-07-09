using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.Views
{
    public partial class AdminTaskGroupTaskDesignView : Window
    {
        private AdminTaskProgramsViewModel _viewModel;

        public AdminTaskGroupTaskDesignView(TaskSession session)
        {
            InitializeComponent();

            _viewModel = new AdminTaskProgramsViewModel(session, ProgramType.Design);
            DataContext = _viewModel;

            ThemeManager.ApplyTheme(Admin_GroupTask_Design_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Design_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateProgramButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CreateProgramCommand.CanExecute(null))
            {
                _viewModel.CreateProgramCommand.Execute(null);
            }
        }

        private void EditProgramButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.EditProgramCommand.CanExecute(_viewModel.SelectedProgram))
            {
                _viewModel.EditProgramCommand.Execute(_viewModel.SelectedProgram);
            }
        }
    }
}