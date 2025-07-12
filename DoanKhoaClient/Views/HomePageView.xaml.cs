using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Services;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.WebSockets;
using DoanKhoaClient.Models;
using DoanKhoaClient.Extensions;
using System.Windows.Threading;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Navigation;


namespace DoanKhoaClient.Views
{
    public partial class HomePageView : Window, INotifyPropertyChanged
    {
        private ActivitiesViewModel _viewModel;
        private bool _isDarkMode;
        private bool isAdminSubmenuOpen;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                OnPropertyChanged();
            }
        }
        public HomePageView()
        {
            _viewModel = new ActivitiesViewModel();
            this.DataContext = _viewModel;
            InitializeComponent();
            this.PreviewMouseDown += Window_PreviewMouseDown;
            // Khởi tạo ViewModel và gán DataContext


            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }
            HomePage_iUsers.SetupAsUserAvatar();


            ThemeManager.ApplyTheme(HomePage_Background);

        }
        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "mailto:doanthanhnien@suctremmt.com",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở email: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void FacebookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "https://www.facebook.com/uit.nc",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở Facebook: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(HomePage_Background);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
        private void FilterDropdownButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                // Cập nhật thuộc tính trong ViewModel để điều khiển Popup
                _viewModel.IsFilterDropdownOpen = true;
            }
        }

        private void FilterDropdownButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                // Cập nhật thuộc tính trong ViewModel để điều khiển Popup
                _viewModel.IsFilterDropdownOpen = false;
            }
        }
        private void Activities_tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var viewModel = DataContext as ActivitiesViewModel;
                if (viewModel != null)
                {
                    viewModel.SearchActivities();
                }
            }
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
        private void FilterPopupBorder_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                var popup = FindParent<Popup>(border);
                if (popup != null)
                {
                    var fadeIn = (Storyboard)popup.Resources["PopupFadeIn"];
                    Storyboard.SetTarget(fadeIn, border);
                    fadeIn.Begin();
                }
            }
        }

        // Hàm hỗ trợ tìm Popup cha
        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            while (parentObject != null)
            {
                if (parentObject is T parent)
                    return parent;
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }
            return null;
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem người dùng có click bên ngoài search box không
            if (!IsMouseOverSearchElements(e.OriginalSource as DependencyObject))
            {
                // Bỏ focus khỏi search box
                Keyboard.ClearFocus();

                // Đóng popup search results nếu đang mở
                var viewModel = DataContext as ActivitiesViewModel;
                if (viewModel != null)
                {
                    viewModel.IsSearchResultOpen = false;
                }

                // Xóa focus khỏi search box
                if (Activities_tbSearch.IsFocused)
                {
                    FocusManager.SetFocusedElement(this, null);
                }
            }
        }

        private bool IsMouseOverSearchElements(DependencyObject element)
        {
            // Kiểm tra xem click có phải trên search box hoặc search results không
            while (element != null)
            {
                if (element == Activities_tbSearch ||
                    (element is Border && element.GetValue(NameProperty)?.ToString() == "SearchResultsBorder"))
                {
                    return true;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }
        protected override void OnClosed(EventArgs e)
        {
            // Cleanup ViewModel timer
            _viewModel?.Cleanup();
            base.OnClosed(e);
        }

        // Thêm vào các event handler
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            SessionService.UpdateSessionActivity();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            SessionService.UpdateSessionActivity();
        }
    }
}

