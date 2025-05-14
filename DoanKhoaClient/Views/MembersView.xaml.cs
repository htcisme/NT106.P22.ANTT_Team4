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


namespace DoanKhoaClient.Views
{
    public partial class MembersView : Window
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

        public MembersView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Members_Background);

            this.SizeChanged += (sender, e) =>
{
    if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
    {
        this.WindowState = WindowState.Normal;
    }
};
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Members_Background);
        }

        private async void HomeMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToHome(this, Members_Background);
        }

        private async void ChatMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToChat(this, Members_Background);
        }

        private async void ActivitiesMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToActivities(this, Members_Background);
        }

        private void MembersMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Đã ở trang Members, không cần điều hướng
        }

        private async void TasksMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToTasks(this, Members_Background);
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

        }

        private void SidebarTasksButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new TasksView();
            win.Show();
            this.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
    }
}