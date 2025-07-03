using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DoanKhoaClient.Views;
namespace DoanKhoaClient.Views
{
    public partial class AdminChatView : Window
    {
        public AdminChatView()
        {
            InitializeComponent();
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
        // Thêm vào AdminChatView.xaml.cs
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
            // Toggle admin submenu
            AdminSubmenu.Visibility = AdminSubmenu.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
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
            // Toggle between light and dark theme
            if (ThemeToggleButton.Source.ToString().Contains("dark"))
            {
                ThemeToggleButton.Source = new BitmapImage(new Uri("/Views/Images/light.png", UriKind.Relative));
                // Apply dark theme
            }
            else
            {
                ThemeToggleButton.Source = new BitmapImage(new Uri("/Views/Images/dark.png", UriKind.Relative));
                // Apply light theme
            }
        }
    }
}