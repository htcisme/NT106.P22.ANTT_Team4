using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Extensions;

namespace DoanKhoaClient.Views
{
    public partial class AdminTasksView : Window
    {
        private AdminTasksViewModel _viewModel;
        private bool _isDarkMode;
        private bool isAdminSubmenuOpen; // Add this field declaration
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
            Admin_Task_iUsers.SetupAsUserAvatar();

            AccessControl.CheckAdminAccess(this);

            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }

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

        private void GoToTasks(object sender, MouseButtonEventArgs e)
        {
            var adminTasksView = new AdminTasksView();
            adminTasksView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminTasksView.Show();
            this.Close();
        }

        private void GoToActivities(object sender, MouseButtonEventArgs e)
        {
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            adminActivitiesView.Show();
            this.Close();
        }

        private void GoToChat(object sender, MouseButtonEventArgs e)
        {
            var chatView = new UserChatView();
            chatView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            chatView.Show();
            this.Close();
        }
        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_Task_Background);
        }


        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SidebarChatButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new UserChatView();
            win.Show();
            this.Close();
        }

        private void SidebarActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ActivitiesView();
            win.Show();
            this.Close();
        }

        private void SidebarMembersButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new MembersView();
            win.Show();
            this.Close();
        }

        private void SidebarTasksButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new TasksView();
            win.Show();
            this.Close();
        }

        private void SidebarAdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle hiển thị submenu admin
            isAdminSubmenuOpen = !isAdminSubmenuOpen;
            AdminSubmenu.Visibility = isAdminSubmenuOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AdminTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var adminTaskView = new AdminTasksView();
            adminTaskView.Show();
            this.Close();
        }

        private void AdminMembersButton_Click(object sender, RoutedEventArgs e)
        {
            var adminMembersView = new AdminMembersView();
            adminMembersView.Show();
            this.Close();
        }

        private void AdminChatButton_Click(object sender, RoutedEventArgs e)
        {
            var adminChatView = new AdminChatView();
            adminChatView.Show();
            this.Close();
        }

        private void AdminActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.Show();
            this.Close();
        }
        // Các phương thức hiện có

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