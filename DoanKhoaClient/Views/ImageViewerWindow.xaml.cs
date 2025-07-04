using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DoanKhoaClient.Views
{
    public partial class ImageViewerWindow : Window
    {
        private string _imageUrl;
        private double _zoomFactor = 1.0;
        private Point _lastPanPoint;
        private bool _isPanning = false;

        public ImageViewerWindow(string imageUrl)
        {
            InitializeComponent();
            _imageUrl = imageUrl;
            LoadImage();
        }

        private async void LoadImage()
        {
            try
            {
                BitmapImage bitmap;

                if (Uri.IsWellFormedUriString(_imageUrl, UriKind.Absolute))
                {
                    // Load from URL
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_imageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }
                else
                {
                    // Load from local file
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_imageUrl, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }

                ImageDisplay.Source = bitmap;

                // Điều chỉnh kích thước window
                double maxWidth = SystemParameters.WorkArea.Width * 0.9;
                double maxHeight = SystemParameters.WorkArea.Height * 0.9;

                if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
                {
                    double aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;

                    if (aspectRatio > 1) // Landscape
                    {
                        this.Width = Math.Min(bitmap.PixelWidth + 100, maxWidth);
                        this.Height = Math.Min(this.Width / aspectRatio + 150, maxHeight);
                    }
                    else // Portrait
                    {
                        this.Height = Math.Min(bitmap.PixelHeight + 150, maxHeight);
                        this.Width = Math.Min(this.Height * aspectRatio + 100, maxWidth);
                    }
                }

                UpdateZoomLevel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể hiển thị hình ảnh: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.Close();
                    break;
                case Key.Add:
                case Key.OemPlus:
                    ZoomIn();
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    ZoomOut();
                    break;
                case Key.D0:
                case Key.NumPad0:
                    FitToWindow();
                    break;
            }
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scaleFactor = e.Delta > 0 ? 1.2 : 0.8;
            _zoomFactor *= scaleFactor;

            // Giới hạn zoom
            _zoomFactor = Math.Max(0.1, Math.Min(_zoomFactor, 10.0));

            ApplyZoom();
            e.Handled = true;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPanning = true;
            _lastPanPoint = e.GetPosition(ImageScrollViewer);
            ImageDisplay.CaptureMouse();
            ImageDisplay.Cursor = Cursors.Hand;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(ImageScrollViewer);

                double deltaX = currentPoint.X - _lastPanPoint.X;
                double deltaY = currentPoint.Y - _lastPanPoint.Y;

                ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - deltaX);
                ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - deltaY);

                _lastPanPoint = currentPoint;
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            ImageDisplay.ReleaseMouseCapture();
            ImageDisplay.Cursor = Cursors.Arrow;
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        private void FitToWindowButton_Click(object sender, RoutedEventArgs e)
        {
            FitToWindow();
        }

        private void ZoomIn()
        {
            _zoomFactor *= 1.2;
            _zoomFactor = Math.Min(_zoomFactor, 10.0);
            ApplyZoom();
        }

        private void ZoomOut()
        {
            _zoomFactor *= 0.8;
            _zoomFactor = Math.Max(_zoomFactor, 0.1);
            ApplyZoom();
        }

        private void FitToWindow()
        {
            _zoomFactor = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            ZoomTransform.ScaleX = _zoomFactor;
            ZoomTransform.ScaleY = _zoomFactor;
            UpdateZoomLevel();
        }

        private void UpdateZoomLevel()
        {
            ZoomLevelText.Text = $"{(_zoomFactor * 100):F0}%";
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "image.jpg",
                    Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif, *.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    if (Uri.IsWellFormedUriString(_imageUrl, UriKind.Absolute))
                    {
                        // Download from URL
                        using (var client = new HttpClient())
                        {
                            client.Timeout = TimeSpan.FromMinutes(5);

                            var response = await client.GetAsync(_imageUrl);
                            if (response.IsSuccessStatusCode)
                            {
                                using (var fileStream = new FileStream(saveDialog.FileName, FileMode.Create))
                                {
                                    await response.Content.CopyToAsync(fileStream);
                                }

                                MessageBox.Show("Tải xuống ảnh thành công!", "Thành công",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show($"Không thể tải ảnh: {response.StatusCode}", "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        // Copy local file
                        File.Copy(_imageUrl, saveDialog.FileName, true);
                        MessageBox.Show("Sao chép ảnh thành công!", "Thành công",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải ảnh: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}