using System;
using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class AdminActivitiesPostView : Window
    {
        private Activity _activity;

        public AdminActivitiesPostView(Activity activity)
        {
            InitializeComponent();
            _activity = activity;
            LoadActivityData();
        }

        private void LoadActivityData()
        {
            // Load dữ liệu của activity vào giao diện
            ActivityTitle.Text = _activity.Title;
            ActivityDate.Text = _activity.Date.ToString("dddd - dd/MM/yyyy"); // Định dạng: Thứ - Ngày/Tháng/Năm
            ActivityContent.Text = _activity.Description;

            // Hiển thị loại hoạt động và trạng thái
            ActivityTypeText.Text = _activity.Type.ToString();
            ActivityStatusText.Text = _activity.Status.ToString();

            // Nếu có hình ảnh, hiển thị
            if (!string.IsNullOrEmpty(_activity.ImgUrl))
            {
                try
                {
                    Uri imageUri = new Uri(_activity.ImgUrl, UriKind.RelativeOrAbsolute);
                    ActivityImage.Source = new System.Windows.Media.Imaging.BitmapImage(imageUri);
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi nếu không thể tải hình ảnh
                    System.Diagnostics.Debug.WriteLine($"Không thể tải hình ảnh: {ex.Message}");
                }
            }
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Xử lý chuyển đổi theme (nếu cần)
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Đóng cửa sổ hiện tại để quay lại màn hình danh sách
            Close();
        }
        
    }
}