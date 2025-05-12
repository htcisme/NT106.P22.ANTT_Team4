using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class EditActivityDialog : Window
    {
        private readonly Activity _originalActivity;
        public Activity Activity { get; private set; }

        public EditActivityDialog(Activity activity)
        {
            InitializeComponent();

            // Store original activity for ID and created date retention
            _originalActivity = activity;

            // Create a clone of the activity for editing
            Activity = new Activity
            {
                Id = activity.Id,
                Title = activity.Title,
                Description = activity.Description,
                Type = activity.Type,
                Date = activity.Date,
                ImgUrl = activity.ImgUrl,
                CreatedAt = activity.CreatedAt,
                Status = activity.Status
            };

            // Set DataContext to the cloned activity
            DataContext = Activity;

            // Hide error message by default
            ErrorMessageBorder.Visibility = Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                // Return the modified activity
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            List<string> errors = new List<string>();

            // Kiểm tra tiêu đề
            if (string.IsNullOrWhiteSpace(Activity.Title))
            {
                errors.Add("Vui lòng nhập tiêu đề hoạt động");
            }
            else if (Activity.Title.Length > 100)
            {
                errors.Add("Tiêu đề không được vượt quá 100 ký tự");
            }

            // Kiểm tra mô tả
            if (string.IsNullOrWhiteSpace(Activity.Description))
            {
                errors.Add("Vui lòng nhập mô tả hoạt động");
            }
            else if (Activity.Description.Length < 1)
            {
                errors.Add("Bạn chuaw nhâp nội dung");
            }

            // Kiểm tra loại hoạt động
            if (TypeComboBox.SelectedItem == null)
            {
                errors.Add("Vui lòng chọn loại hoạt động");
            }

            // Kiểm tra ngày diễn ra - chỉ kiểm tra khi trạng thái là Upcoming hoặc Ongoing
            if (Activity.Date < DateTime.Now.Date &&
                Activity.Status != ActivityStatus.Completed)
            {
                errors.Add("Ngày diễn ra không được trong quá khứ khi trạng thái là Sắp diễn ra hoặc Đang diễn ra");
            }

            // Kiểm tra URL hình ảnh (nếu có)
            if (!string.IsNullOrWhiteSpace(Activity.ImgUrl) && !Uri.TryCreate(Activity.ImgUrl, UriKind.Absolute, out _))
            {
                errors.Add("URL hình ảnh không hợp lệ");
            }

            // Kiểm tra trạng thái
            if (StatusComboBox.SelectedItem == null)
            {
                errors.Add("Vui lòng chọn trạng thái hoạt động");
            }

            // Hiển thị danh sách lỗi nếu có
            if (errors.Count > 0)
            {
                StringBuilder errorMessage = new StringBuilder();
                errorMessage.AppendLine("Vui lòng sửa các lỗi sau:");

                foreach (var error in errors)
                {
                    errorMessage.AppendLine($"• {error}");
                }

                ErrorMessageText.Text = errorMessage.ToString();
                ErrorMessageBorder.Visibility = Visibility.Visible;

                return false;
            }

            // Không có lỗi
            ErrorMessageBorder.Visibility = Visibility.Collapsed;
            return true;
        }
    }
}