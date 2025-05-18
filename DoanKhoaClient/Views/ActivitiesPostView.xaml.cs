using System;
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
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class ActivitiesPostView : Window
    {
        private readonly ActivitiesPostViewModel _viewModel;
        private readonly UserService _userService;
        private readonly ActivityService _activityService;

        public ActivitiesPostView(Activity activity)
        {
            InitializeComponent();

            // Khởi tạo các services
            _activityService = new ActivityService();
            _userService = new UserService(_activityService);

            // Khởi tạo ViewModel với activity và services
            _viewModel = new ActivitiesPostViewModel(activity, _userService, _activityService);

            // Gán ViewModel làm DataContext
            this.DataContext = _viewModel;

            // Áp dụng theme
            ThemeManager.ApplyTheme(ActivitiesPost_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(ActivitiesPost_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Sử dụng BackCommand từ ViewModel để đảm bảo tính nhất quán
            if (_viewModel.BackCommand.CanExecute(null))
            {
                _viewModel.BackCommand.Execute(null);
            }
            else
            {
                // Fallback nếu command không hoạt động
                this.Close();
            }
        }

        private void SidebarHomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up ViewModel trước khi điều hướng
            _viewModel.Cleanup();

            var win = new HomePageView();
            win.Show();
            this.Close();
        }

        private void SidebarChatButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up ViewModel trước khi điều hướng
            _viewModel.Cleanup();

            var win = new UserChatView();
            win.Show();
            this.Close();
        }

        private void SidebarActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up ViewModel trước khi điều hướng
            _viewModel.Cleanup();

            var win = new ActivitiesView();
            win.Show();
            this.Close();
        }

        private void SidebarMembersButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up ViewModel trước khi điều hướng
            _viewModel.Cleanup();

            var win = new MembersView();
            win.Show();
            this.Close();
        }

        private void SidebarTasksButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up ViewModel trước khi điều hướng
            _viewModel.Cleanup();

            var win = new TasksView();
            win.Show();
            this.Close();
        }

        private void FilterDropdownButton_Checked(object sender, RoutedEventArgs e)
        {
            // Xử lý chức năng filter ở cấp ActivitiesViewModel
            var activitiesViewModel = FindResource("ActivitiesViewModel") as ActivitiesViewModel;
            if (activitiesViewModel != null)
            {
                activitiesViewModel.IsFilterDropdownOpen = true;
            }
        }

        private void FilterDropdownButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // Xử lý chức năng filter ở cấp ActivitiesViewModel
            var activitiesViewModel = FindResource("ActivitiesViewModel") as ActivitiesViewModel;
            if (activitiesViewModel != null)
            {
                activitiesViewModel.IsFilterDropdownOpen = false;
            }
        }

        private void Activities_tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Xử lý chức năng tìm kiếm ở cấp ActivitiesViewModel
                var activitiesViewModel = FindResource("ActivitiesViewModel") as ActivitiesViewModel;
                if (activitiesViewModel != null)
                {
                    activitiesViewModel.FilterActivities();
                }
            }
        }

        private void FilterPopupBorder_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                var popup = FindParent<Popup>(border);
                if (popup != null && popup.Resources.Contains("PopupFadeIn"))
                {
                    var fadeIn = (Storyboard)popup.Resources["PopupFadeIn"];
                    Storyboard.SetTarget(fadeIn, border);
                    fadeIn.Begin();
                }
            }
        }

        // Hàm hỗ trợ tìm Popup cha
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
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

        // Clean up khi cửa sổ bị đóng
        protected override void OnClosed(EventArgs e)
        {
            // Giải phóng tài nguyên của ViewModel
            _viewModel.Cleanup();
            base.OnClosed(e);
        }
    }
}