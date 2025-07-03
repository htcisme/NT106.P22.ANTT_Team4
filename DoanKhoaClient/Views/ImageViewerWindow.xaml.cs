// Tạo file mới: ImageViewerWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DoanKhoaClient.Views
{
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow(string imageUrl)
        {
            InitializeComponent();

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // Điều chỉnh kích thước window theo kích thước ảnh
                double maxWidth = SystemParameters.WorkArea.Width * 0.85;
                double maxHeight = SystemParameters.WorkArea.Height * 0.85;

                double width = bitmap.PixelWidth;
                double height = bitmap.PixelHeight;

                if (width > maxWidth)
                {
                    double ratio = maxWidth / width;
                    width = maxWidth;
                    height *= ratio;
                }

                if (height > maxHeight)
                {
                    double ratio = maxHeight / height;
                    height = maxHeight;
                    width *= ratio;
                }

                this.Width = width + 30; // Thêm padding
                this.Height = height + 50; // Thêm padding và titlebar

                ImageDisplay.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể hiển thị hình ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
    }
}