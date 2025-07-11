using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Extensions;
using System.Diagnostics;

namespace DoanKhoaClient.Views
{
    public partial class AdminMembersView : Window
    {
        private bool _isDarkMode;
        private bool isAdminSubmenuOpen; // Add this field declaration
        public AdminMembersView()
        {
            InitializeComponent();

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_Members_Background);

            // Kiểm tra quyền truy cập
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

            Admin_Members_iUsers.SetupAsUserAvatar();
        }

        // Thêm method này để xử lý click vào ListViewItem
        private void ListViewItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("ListViewItem_MouseLeftButtonDown called");

            if (sender is System.Windows.Controls.ListViewItem listViewItem)
            {
                Debug.WriteLine("Sender is ListViewItem");

                // Kiểm tra xem có click trực tiếp vào checkbox không
                var hitTest = e.OriginalSource as System.Windows.DependencyObject;
                Debug.WriteLine($"OriginalSource: {hitTest?.GetType().Name}");

                // Nếu click vào checkbox hoặc button, không xử lý
                if (IsClickableControl(hitTest))
                {
                    Debug.WriteLine("Clicked on clickable control, ignoring");
                    return;
                }

                // Lấy User object từ DataContext của ListViewItem
                if (listViewItem.DataContext is User user)
                {
                    Debug.WriteLine($"Found user: {user.DisplayName ?? user.Username}");
                    Debug.WriteLine($"Current IsSelected: {user.IsSelected}");

                    // Đảo ngược trạng thái IsSelected - điều này đảm bảo click 2 lần sẽ bỏ chọn
                    user.IsSelected = !user.IsSelected;

                    Debug.WriteLine($"New IsSelected: {user.IsSelected}");

                    // Đánh dấu event đã được xử lý để tránh selection behavior mặc định
                    e.Handled = true;
                }
                else
                {
                    Debug.WriteLine("DataContext is not User");
                }
            }
            else
            {
                Debug.WriteLine("Sender is not ListViewItem");
            }
        }

        // Helper method để kiểm tra xem element có phải là control có thể click được không
        private bool IsClickableControl(System.Windows.DependencyObject element)
        {
            while (element != null)
            {
                // Kiểm tra các control có thể click được
                if (element is System.Windows.Controls.CheckBox ||
                    element is System.Windows.Controls.Button)
                {
                    Debug.WriteLine($"Found clickable control: {element.GetType().Name}");
                    return true;
                }

                // Di chuyển lên parent trong visual tree
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
            return false;
        }

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
            ThemeManager.ToggleTheme(Admin_Members_Background);
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
    }
}