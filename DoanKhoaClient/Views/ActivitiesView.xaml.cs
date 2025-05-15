using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DoanKhoaClient.Extensions;
namespace DoanKhoaClient.Views
{
    public partial class ActivitiesView : Window, INotifyPropertyChanged
    {
        private ActivitiesViewModel _viewModel;
        private bool _isDarkMode;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                OnPropertyChanged();
            }
        }
        private bool isAdminSubmenuOpen = false;
        public ActivitiesView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Activities_Background);
            IsDarkMode = ThemeManager.IsDarkMode;
            _viewModel = new ActivitiesViewModel();
            this.DataContext = _viewModel;
            LightHomePage_iUsers.SetupAsUserAvatar();
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
};
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Activities_Background);
            IsDarkMode = ThemeManager.IsDarkMode;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void HomeMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToHome(this, Activities_Background);
        }

        private async void ChatMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToChat(this, Activities_Background);
        }

        private void ActivitiesMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private async void MembersMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var win = new MembersView();
            win.Show();
            this.Close();

        }

        private async void TasksMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToTasks(this, Activities_Background);
        }

        private void LightHomePage_iSearch_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Thực hiện tìm kiếm hoặc mở popup tìm kiếm
            if (_viewModel != null)
            {
                _viewModel.FilterActivities();
            }
        }

        // Thêm handler cho FilterDropdownButton
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
        private void SidebarAdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle hiển thị submenu admin
            isAdminSubmenuOpen = !isAdminSubmenuOpen;
            AdminSubmenu.Visibility = isAdminSubmenuOpen ? Visibility.Visible : Visibility.Collapsed;
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

        private void sortByButton_Click(object sender, RoutedEventArgs e)
        {
            SortFilterPopup.IsOpen = true;
        }

        private void ApplySortButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                string sortType = ((ComboBoxItem)SortTypeComboBox.SelectedItem).Content.ToString();
                if (int.TryParse(SortAmountTextBox.Text, out int amount))
                {
                    _viewModel.SortActivities(sortType, amount);
                    sortByButton.Content = $"Sắp xếp: {amount} {sortType.ToLower()}";
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập số lượng hợp lệ", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            SortFilterPopup.IsOpen = false;
        }

        private void ClearSortButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                // Load lại toàn bộ activities gốc từ database
                _viewModel.LoadActivitiesAsync();
                sortByButton.Content = "Sắp xếp theo:";
            }
            SortFilterPopup.IsOpen = false;
        }
        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new HomePageView();
            win.Show();
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
    }
}