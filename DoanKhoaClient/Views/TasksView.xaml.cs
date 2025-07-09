using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Extensions;
using System.Windows.Controls;
using System.Windows.Media;
namespace DoanKhoaClient.Views
{
    public partial class TasksView : Window
    {
        private bool isAdminSubmenuOpen = false;
        private ActivitiesViewModel _viewModel;

        public TasksView()
        {
            InitializeComponent();
            this.PreviewMouseDown += Window_PreviewMouseDown;
            _viewModel = new ActivitiesViewModel();
            this.DataContext = _viewModel;
            ThemeManager.ApplyTheme(Task_Background);
            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }
            this.SizeChanged += (sender, e) =>
{
    if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
    {
        this.WindowState = WindowState.Normal;
    }
    Task_iUsers.SetupAsUserAvatar();
};
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Task_Background);
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
        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new HomePageView();
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
        private void Activities_tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var viewModel = DataContext as ActivitiesViewModel;
                if (viewModel != null)
                {
                    viewModel.FilterActivities();
                }
            }
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

        private void DesignTeam_Click(object sender, RoutedEventArgs e)
        {
            var designView = new TasksGroupTaskDesignView();
            designView.Show();
            // Không đóng window hiện tại để có thể quay lại
        }

        private void EventTeam_Click(object sender, RoutedEventArgs e)
        {
            var eventView = new TasksGroupTaskEventView();
            eventView.Show();
        }

        private void StudyTeam_Click(object sender, RoutedEventArgs e)
        {
            var studyView = new TasksGroupTaskStudyView();
            studyView.Show();
        }
        // Thêm các phương thức FilterDropdownButton_Unchecked và FilterPopupBorder_Loaded đã có trong code
    }


}