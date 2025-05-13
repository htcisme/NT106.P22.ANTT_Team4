using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.ViewModels;
using System.Windows.Input;
using System.Collections.ObjectModel;


namespace DoanKhoaClient.Views
{
    public partial class HomePageView : Window
    {
        private ActivitiesViewModel _viewModel;
        public HomePageView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(HomePage_Background);
            _viewModel = new ActivitiesViewModel();
            this.DataContext = _viewModel;
            this.Loaded += async (s, e) =>
            {
                await _viewModel.LoadActivitiesAsync();
            };
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
            ThemeManager.ToggleTheme(HomePage_Background);
        }
        private void HomeMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Đã ở trang Home, không cần điều hướng
        }

        private async void ChatMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToChat(this, HomePage_Background);
        }

        private async void ActivitiesMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToActivities(this, HomePage_Background);
        }

        private async void MembersMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToMembers(this, HomePage_Background);
        }

        private async void TasksMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToTasks(this, HomePage_Background);
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
                    viewModel.FilterActivities();


                }
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
    }
}

