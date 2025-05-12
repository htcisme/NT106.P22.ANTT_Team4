using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
namespace DoanKhoaClient.Views
{
    public partial class CreateActivityDialog : Window
    {
        public Activity Activity { get; private set; }

        public CreateActivityDialog()
        {
            InitializeComponent();
            Activity = new Activity
            {
                Id = null, // Để server tự tạo ID
                CreatedAt = DateTime.Now,
                Date = DateTime.Now,
                Status = ActivityStatus.Upcoming,
                Type = ActivityType.Academic // Set default type
            };
            DataContext = Activity;

            // Ẩn border thông báo lỗi
            ErrorMessageBorder.Visibility = Visibility.Collapsed;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
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
                errors.Add("Bạn chưa nhập nội dung");
            }

            // Kiểm tra loại hoạt động
            if (TypeComboBox.SelectedItem == null)
            {
                errors.Add("Vui lòng chọn loại hoạt động");
            }

            // Kiểm tra ngày diễn ra - cho phép trong quá khứ khi trạng thái là Completed
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

    public static class ActivityTypeEnum
    {
        public static Array Values => Enum.GetValues(typeof(ActivityType));
    }

    public static class ActivityStatusEnum
    {
        public static Array Values => Enum.GetValues(typeof(ActivityStatus));
    }
}