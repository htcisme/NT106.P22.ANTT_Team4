using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DoanKhoaClient.Models;
using System.Windows.Media;
using DoanKhoaClient.ViewModels;

namespace DoanKhoaClient.Views
{
    public partial class AdminActivitiesView : Window
    {
        private AdminActivitiesViewModel _viewModel;

        public AdminActivitiesView()
        {
            InitializeComponent();
            _viewModel = new AdminActivitiesViewModel();
            DataContext = _viewModel;
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Xử lý chuyển đổi theme (có thể bổ sung sau)
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