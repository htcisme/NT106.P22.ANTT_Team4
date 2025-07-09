using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DoanKhoaClient.Views;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Extensions;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Views
{
    public partial class AdminChatView : Window, INotifyPropertyChanged
    {
        private bool _isDarkMode;
        private bool isAdminSubmenuOpen = false;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                OnPropertyChanged();
            }
        }

        public AdminChatView()
        {
            InitializeComponent();

            // Remove animation - set final state directly like ActivitiesView
            AdminChat_Background.Opacity = 1;
            AdminChat_Background.Margin = new Thickness(0);

            // Apply theme management like ActivitiesView
            ThemeManager.ApplyTheme(AdminChat_Background);
            IsDarkMode = ThemeManager.IsDarkMode;

            // Setup user avatar like ActivitiesView
            // Assuming there's a user avatar image in the top bar
            if (FindName("LightHomePage_iUsers") is Image userAvatar)
            {
                userAvatar.SetupAsUserAvatar();
            }

            // Show admin menu by default since this is admin view
            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                // Redirect non-admin users
                MessageBox.Show("Bạn không có quyền truy cập vào khu vực quản trị!", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                var homeView = new HomePageView();
                homeView.Show();
                this.Close();
                return;
            }

            this.SizeChanged += (sender, e) =>
            {
                if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
                {
                    this.WindowState = WindowState.Normal;
                }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Home
            var homeView = new HomePageView();
            homeView.Show();
            this.Close();
        }

        private void SidebarChatButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Chat
            var chatView = new UserChatView();
            chatView.Show();
            this.Close();
        }

        private void SidebarActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Activities
            var activitiesView = new ActivitiesView();
            activitiesView.Show();
            this.Close();
        }

        private void SidebarMembersButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Members
            var membersView = new MembersView();
            membersView.Show();
            this.Close();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Source is BitmapImage bitmapImage)
            {
                ImageViewerWindow imageViewer = new ImageViewerWindow(bitmapImage.UriSource.ToString());
                imageViewer.Show();
            }
        }

        private void SidebarTasksButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Tasks
            var tasksView = new TasksView();
            tasksView.Show();
            this.Close();
        }

        private void SidebarAdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle admin submenu like ActivitiesView
            isAdminSubmenuOpen = !isAdminSubmenuOpen;
            AdminSubmenu.Visibility = isAdminSubmenuOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AdminTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Admin Task Management
            var adminTaskView = new AdminTasksView();
            adminTaskView.Show();
            this.Close();
        }

        private void AdminMembersButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Admin Members Management
            var adminMembersView = new AdminMembersView();
            adminMembersView.Show();
            this.Close();
        }

        private void AdminChatButton_Click(object sender, RoutedEventArgs e)
        {
            // Already on AdminChatView - no need to navigate
        }

        private void AdminActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Admin Activities Management
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.Show();
            this.Close();
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Use ThemeManager like ActivitiesView
            ThemeManager.ToggleTheme(AdminChat_Background);
            IsDarkMode = ThemeManager.IsDarkMode;
        }
    }
}