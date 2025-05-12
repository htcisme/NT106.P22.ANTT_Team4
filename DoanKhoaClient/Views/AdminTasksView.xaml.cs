using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
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

            // Kiểm tra quyền truy cập
            AccessControl.CheckAdminAccess(this);

            _viewModel = new AdminTasksViewModel();
            DataContext = _viewModel;
            ThemeManager.ApplyTheme(Admin_Task_Background);

            // Thêm xử lý hướng dẫn và kiểm tra tài nguyên
            Loaded += AdminTasksView_Loaded;

            // Check window size
            this.SizeChanged += (sender, e) =>
            {
                if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
                {
                    this.WindowState = WindowState.Normal;
                }
            };
        }
        private void GoToDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboardView = new AdminDashboardView();
            dashboardView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dashboardView.Show();
            this.Close();
        }

        private void AdminTasksView_Loaded(object sender, RoutedEventArgs e)
        {
            // Kiểm tra tài nguyên và tiền tải các view cần thiết
            try
            {
                // Đảm bảo tài nguyên được tải đúng cách
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("/DoanKhoaClient;component/Resources/TaskViewResources.xaml", UriKind.Relative)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải tài nguyên: {ex.Message}");
            }
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

        private void SessionsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedSession != null &&
                _viewModel.ViewSessionDetailsCommand.CanExecute(_viewModel.SelectedSession))
            {
                _viewModel.ViewSessionDetailsCommand.Execute(_viewModel.SelectedSession);
            }
        }

        private void SessionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Đảm bảo command được cập nhật khi chọn session thay đổi
            CommandManager.InvalidateRequerySuggested();
        }
    }
}