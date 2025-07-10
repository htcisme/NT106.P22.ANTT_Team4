using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DoanKhoaClient.Models;
using System.Windows.Media;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Extensions;
namespace DoanKhoaClient.Views
{
    public partial class AdminActivitiesView : Window
    {
        private bool _isDarkMode;
        private bool isAdminSubmenuOpen; // Add this field declaration
        private AdminActivitiesViewModel _viewModel;

        public AdminActivitiesView()
        {
            InitializeComponent();

            // Kiểm tra quyền truy cập
            AccessControl.CheckAdminAccess(this);

            _viewModel = new AdminActivitiesViewModel();
            DataContext = _viewModel;
            HomePage_iUsers.SetupAsUserAvatar();
            // Check window size
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

        // Thêm code để điều hướng đến các trang admin khác

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
            ThemeManager.ToggleTheme(Admin_Activities_Background);
        }


        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {
            var homeView = new HomePageView();
            homeView.Show();
            this.Close();
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


        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // IsAllSelected đã được binding với CheckBox, nên tự động xử lý trong ViewModel
            // Tuy nhiên, có thể cần cập nhật UI
            CommandManager.InvalidateRequerySuggested();
        }

        private void ItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Khi checkbox của một mục thay đổi, cập nhật trạng thái IsAllSelected và HasSelectedItems
            _viewModel.UpdateIsAllSelected();
            _viewModel.UpdateHasSelectedItems();
        }

        private void BatchEditButton_Click(object sender, RoutedEventArgs e)
        {
            // Thực hiện lệnh chỉnh sửa hàng loạt
            _viewModel.BatchEditCommand.Execute(null);
        }

        private void BatchDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Thực hiện lệnh xóa hàng loạt
            _viewModel.BatchDeleteCommand.Execute(null);
        }

        private void ViewDetailButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy hoạt động từ Tag của Button
            var button = sender as Button;
            if (button != null && button.Tag is Activity activity)
            {
                // Mở trang chi tiết hoạt động
                _viewModel.ViewDetailCommand.Execute(activity);
            }
        }

        private void ActivitiesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Cập nhật SelectedActivity trong ViewModel
            if (ActivitiesListView.SelectedItem is Activity selectedActivity)
            {
                _viewModel.SelectedActivity = selectedActivity;
            }
        }

        private void ListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item)
            {
                // Kiểm tra xem click có nằm trong vùng checkbox không
                var checkbox = FindVisualChild<CheckBox>(item);
                if (checkbox != null)
                {
                    // Lấy vị trí click tương đối với ListViewItem
                    var clickPosition = e.GetPosition(item);
                    // Lấy vị trí checkbox tương đối với ListViewItem
                    var checkboxPosition = checkbox.TransformToAncestor(item).Transform(new Point(0, 0));

                    // Kiểm tra xem click có nằm trong vùng checkbox không
                    if (clickPosition.X >= checkboxPosition.X &&
                        clickPosition.X <= checkboxPosition.X + checkbox.ActualWidth &&
                        clickPosition.Y >= checkboxPosition.Y &&
                        clickPosition.Y <= checkboxPosition.Y + checkbox.ActualHeight)
                    {
                        checkbox.IsChecked = !checkbox.IsChecked;
                        e.Handled = true;
                    }
                }
            }
        }

        private void FilterDropdownButton_Checked(object sender, RoutedEventArgs e)
        {
            ((ActivitiesViewModel)DataContext).IsFilterDropdownOpen = true;
        }

        private void FilterDropdownButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ((ActivitiesViewModel)DataContext).IsFilterDropdownOpen = false;
        }

        // Helper method để tìm control trong visual tree
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T found)
                    return found;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}