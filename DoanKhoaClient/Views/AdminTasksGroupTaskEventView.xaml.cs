using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; 

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
        private void ProgramsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is TaskProgram selectedProgram)
            {
                // Lấy ViewModel từ DataContext
                var viewModel = DataContext as AdminTaskProgramsViewModel;
                if (viewModel != null && viewModel.ViewProgramDetailsCommand != null)
                {
                    // Gọi lệnh xem chi tiết chương trình từ ViewModel
                    viewModel.ViewProgramDetailsCommand.Execute(selectedProgram);
                }
            }
        }
    }
}