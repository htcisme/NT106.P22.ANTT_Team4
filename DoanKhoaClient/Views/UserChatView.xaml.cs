using System.Windows;
using DoanKhoaClient.Helpers;
using System.Windows.Input;
using DoanKhoaClient.Extensions;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using DoanKhoaClient.Models;
using System.Net.Http;
using System.IO;
using DoanKhoaClient.Services;
using DoanKhoaClient.ViewModels;
namespace DoanKhoaClient.Views
{
    public partial class UserChatView : Window
    {
        private bool isAdminSubmenuOpen = false;
        public UserChatView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                // Đặt mọi thứ ở trạng thái cuối của animation
                Chat_Background.Opacity = 1;
                Chat_Background.Margin = new Thickness(0);
            }
            else
            {
                // Đặt giá trị ban đầu cho animations khi chạy thật
                Chat_Background.Opacity = 0;
                Chat_Background.Margin = new Thickness(0, 30, 0, 0);
                this.DataContext = new UserChatViewModel();
            }
            if (AccessControl.IsAdmin())
            {
                SidebarAdminButton.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarAdminButton.Visibility = Visibility.Collapsed;
                AdminSubmenu.Visibility = Visibility.Collapsed;
            }
            ThemeManager.ApplyTheme(Chat_Background);
            this.SizeChanged += (sender, e) =>
{
    if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
    {
        this.WindowState = WindowState.Normal;
    }
    UserAvatar.SetupAsUserAvatar();
};
        }

        private void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.Tag is Message message)
                {
                    if (DataContext is UserChatViewModel viewModel)
                    {
                        // Debug log
                        System.Diagnostics.Debug.WriteLine($"Edit message clicked: {message.Id}, Content: {message.Content}");

                        // Gọi command từ ViewModel
                        if (viewModel.EditMessageCommand.CanExecute(message))
                        {
                            viewModel.EditMessageCommand.Execute(message);
                        }
                        else
                        {
                            MessageBox.Show("Bạn không thể chỉnh sửa tin nhắn này!", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chỉnh sửa: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.Tag is Message message)
                {
                    if (DataContext is UserChatViewModel viewModel)
                    {
                        // Debug log
                        System.Diagnostics.Debug.WriteLine($"Delete message clicked: {message.Id}, Content: {message.Content}");

                        // Gọi command từ ViewModel
                        if (viewModel.DeleteMessageCommand.CanExecute(message))
                        {
                            viewModel.DeleteMessageCommand.Execute(message);
                        }
                        else
                        {
                            MessageBox.Show("Bạn không thể xóa tin nhắn này!", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Image image)
                {
                    string imageUrl = image.Tag as string;

                    if (string.IsNullOrEmpty(imageUrl) && image.Source is BitmapImage bitmapImage)
                    {
                        imageUrl = bitmapImage.UriSource?.ToString();
                    }

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Tạo context menu cho ảnh
                        var contextMenu = new ContextMenu();

                        // Menu item xem ảnh
                        var viewMenuItem = new MenuItem { Header = "Xem ảnh" };
                        viewMenuItem.Click += (s, args) =>
                        {
                            try
                            {
                                var imageViewer = new ImageViewerWindow(imageUrl);
                                imageViewer.Owner = this;
                                imageViewer.Show();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Không thể mở ảnh: {ex.Message}", "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        };

                        // Menu item tải xuống ảnh
                        var downloadMenuItem = new MenuItem { Header = "Tải xuống" };
                        downloadMenuItem.Click += async (s, args) =>
                        {
                            await DownloadImageAsync(imageUrl, image.DataContext as Attachment);
                        };

                        contextMenu.Items.Add(viewMenuItem);
                        contextMenu.Items.Add(downloadMenuItem);

                        // Hiển thị context menu
                        contextMenu.IsOpen = true;
                        contextMenu.PlacementTarget = image;
                        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DownloadImageAsync(string imageUrl, Attachment attachment)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = attachment?.FileName ?? "image.jpg",
                    Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif|All files (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMinutes(5);

                        var response = await client.GetAsync(imageUrl);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải ảnh: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem)
                {
                    Message message = null;

                    // Thử lấy từ Tag trước
                    if (menuItem.Tag is Message tagMessage)
                    {
                        message = tagMessage;
                    }
                    // Nếu không có thì thử từ DataContext
                    else if (menuItem.DataContext is Message dataContextMessage)
                    {
                        message = dataContextMessage;
                    }
                    // Thử lấy từ PlacementTarget
                    else if (menuItem.Parent is ContextMenu contextMenu &&
                             contextMenu.PlacementTarget is FrameworkElement target &&
                             target.DataContext is Message targetMessage)
                    {
                        message = targetMessage;
                    }

                    if (message != null && !string.IsNullOrEmpty(message.Content))
                    {
                        Clipboard.SetText(message.Content);
                        MessageBox.Show("Đã sao chép tin nhắn!", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Không có nội dung để sao chép!", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi sao chép: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Chat_Background);
        }
        private async void ChatMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }
        private async void HomeMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToHome(this, Chat_Background);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Attachment attachment)
            {
                DownloadFileDirectly(attachment);
            }
        }

        // Add this method to UserChatView.xaml.cs
        private void AttachmentImage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Image image && image.DataContext is Attachment attachment)
                {
                    System.Diagnostics.Debug.WriteLine($"Loading image: {attachment.FileName}");

                    if (!string.IsNullOrEmpty(attachment.FileUrl))
                    {
                        // Đảm bảo URL là absolute
                        string imageUrl = attachment.FileUrl;
                        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                        {
                            imageUrl = $"http://localhost:5299/Uploads/{Path.GetFileName(imageUrl)}";
                        }

                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imageUrl);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.EndInit();

                        image.Source = bitmap;
                        image.Tag = imageUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
            }
        }

        private async void TestImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is UserChatViewModel viewModel)
                {
                    foreach (var message in viewModel.Messages)
                    {
                        if (message.Attachments != null)
                        {
                            foreach (var attachment in message.Attachments)
                            {
                                if (attachment.IsImage)
                                {
                                    // Show attachment info
                                    MessageBox.Show($"Found image attachment:\nFilename: {attachment.FileName}\nURL: {attachment.FileUrl}\nIsImage: {attachment.IsImage}");

                                    // Try to download it
                                    using (HttpClient client = new HttpClient())
                                    {
                                        try
                                        {
                                            string url = attachment.FileUrl;
                                            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                                            {
                                                url = $"http://localhost:5299/Uploads/{Path.GetFileName(url)}";
                                            }

                                            var response = await client.GetAsync(url);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                MessageBox.Show($"Image exists on server: {url}", "Success");
                                            }
                                            else
                                            {
                                                MessageBox.Show($"Image not found: {url}\nStatus: {response.StatusCode}", "Error");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show($"Error testing image: {ex.Message}", "Error");
                                        }
                                    }

                                    return; // Only test first image
                                }
                            }
                        }
                    }
                    MessageBox.Show("No image attachments found to test");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test error: {ex.Message}", "Error");
            }
        }
        private async void DownloadFileDirectly(Attachment attachment)
        {

            try
            {
                if (attachment == null)
                {
                    MessageBox.Show("Không thể tải file: Attachment là null", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Log attachment details
                System.Diagnostics.Debug.WriteLine($"Attempting to download attachment: ID={attachment.Id}, FileName={attachment.FileName}");
                System.Diagnostics.Debug.WriteLine($"Original FileUrl: {attachment.FileUrl}");

                // Ensure we have a valid URL
                string fileUrl = attachment.FileUrl;
                if (string.IsNullOrEmpty(fileUrl))
                {
                    // Try to construct the URL from filename if FileUrl is empty
                    if (!string.IsNullOrEmpty(attachment.FileName))
                    {
                        fileUrl = $"http://localhost:5299/Uploads/{attachment.FileName}";
                        System.Diagnostics.Debug.WriteLine($"Generated fallback URL: {fileUrl}");
                    }
                    else
                    {
                        MessageBox.Show("Không thể tải file: Không có URL và tên file", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else if (!Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
                {
                    // Fix relative URLs
                    fileUrl = $"http://localhost:5299{(fileUrl.StartsWith("/") ? "" : "/")}{fileUrl}";
                    System.Diagnostics.Debug.WriteLine($"Fixed relative URL to: {fileUrl}");
                }

                // Hiện dialog save
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = attachment.FileName,
                    Filter = "All files (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Rest of the download code with the fixed URL
                    string saveFilePath = saveDialog.FileName;

                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMinutes(5);
                        System.Diagnostics.Debug.WriteLine($"Sending request to: {fileUrl}");

                        using (var response = await client.GetAsync(fileUrl))
                        {
                            System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

                            if (response.IsSuccessStatusCode)
                            {
                                // Download successful
                                using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
                                {
                                    await response.Content.CopyToAsync(fileStream);
                                }
                                MessageBox.Show($"Đã tải xuống {attachment.FileName} thành công",
                                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                MessageBox.Show($"Không thể tải file: {response.StatusCode} - {errorContent}",
                                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void ActivitiesMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToActivities(this, Chat_Background);
        }

        private async void MembersMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToMembers(this, Chat_Background);
        }

        private async void TasksMenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await NavigationHelper.NavigateToTasks(this, Chat_Background);
        }


        private void SidebarAdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle hiển thị submenu admin
            isAdminSubmenuOpen = !isAdminSubmenuOpen;
            AdminSubmenu.Visibility = isAdminSubmenuOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AdminTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var adminTaskView = new AdminTasksView();
            adminTaskView.Show();
            this.Close();
        }

        private void AdminMembersButton_Click(object sender, RoutedEventArgs e)
        {
            var adminMembersView = new AdminMembersView();
            adminMembersView.Show();
            this.Close();
        }

        private void AdminChatButton_Click(object sender, RoutedEventArgs e)
        {
            var adminChatView = new AdminChatView();
            adminChatView.Show();
            this.Close();
        }

        private void AdminActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.Show();
            this.Close();
        }

    }
}