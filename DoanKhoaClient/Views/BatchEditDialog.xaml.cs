// BatchEditDialog.xaml.cs
using System;
using System.Collections.Generic;
using System.Windows;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class BatchEditDialog : Window
    {
        public Dictionary
<string, object> UpdatedFields
        { get; private set; }
        private List
    <Activity> _selectedActivities;

        public BatchEditDialog(List
        <Activity> selectedActivities)
        {
            InitializeComponent();
            _selectedActivities = selectedActivities;
            UpdatedFields = new Dictionary
            <string, object>();

            // Khởi tạo các giá trị mặc định
            DatePicker.SelectedDate = DateTime.Today;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem có trường nào được chọn không
            if (!TypeCheckBox.IsChecked.GetValueOrDefault() &&
                !StatusCheckBox.IsChecked.GetValueOrDefault() &&
                !DateCheckBox.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một trường để cập nhật.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Thu thập các giá trị từ điều khiển
            if (TypeCheckBox.IsChecked.GetValueOrDefault() && TypeComboBox.SelectedItem != null)
            {
                UpdatedFields["Type"] = TypeComboBox.SelectedItem;
            }

            if (StatusCheckBox.IsChecked.GetValueOrDefault() && StatusComboBox.SelectedItem != null)
            {
                UpdatedFields["Status"] = StatusComboBox.SelectedItem;
            }

            if (DateCheckBox.IsChecked.GetValueOrDefault() && DatePicker.SelectedDate.HasValue)
            {
                UpdatedFields["Date"] = DatePicker.SelectedDate.Value;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}