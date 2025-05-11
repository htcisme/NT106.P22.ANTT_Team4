using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;

namespace DoanKhoaClient.Views
{
    public partial class AdminTasksView : Window
    {
        private AdminTasksViewModel _viewModel;

        public AdminTasksView()
        {
            InitializeComponent();
            _viewModel = new AdminTasksViewModel();
            DataContext = _viewModel;
            ThemeManager.ApplyTheme(Admin_Task_Background);
        }

        // Thêm các phương thức xử lý sự kiện
        private void Home_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.NavigateToHomeCommand.CanExecute(null))
            {
                _viewModel.NavigateToHomeCommand.Execute(null);
            }
        }

        private void Chat_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.NavigateToChatCommand.CanExecute(null))
            {
                _viewModel.NavigateToChatCommand.Execute(null);
            }
        }

        private void Activities_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.NavigateToActivitiesCommand.CanExecute(null))
            {
                _viewModel.NavigateToActivitiesCommand.Execute(null);
            }
        }

        private void Members_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.NavigateToMembersCommand.CanExecute(null))
            {
                _viewModel.NavigateToMembersCommand.Execute(null);
            }
        }

        // Các phương thức hiện có
        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_Task_Background);
        }

        private void CreateSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CreateSessionCommand.CanExecute(null))
            {
                _viewModel.CreateSessionCommand.Execute(null);
            }
        }

        private void EditSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.EditSessionCommand.CanExecute(_viewModel.SelectedSession))
            {
                _viewModel.EditSessionCommand.Execute(_viewModel.SelectedSession);
            }
        }
    }
}