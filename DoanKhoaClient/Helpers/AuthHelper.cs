using System;
using System.IO;
using System.Windows;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;

namespace DoanKhoaClient.Helpers
{
    public static class AuthHelper
    {
        public static void Logout()
        {
            try
            {
                // Xóa thông tin đăng nhập từ Application.Current.Properties
                if (Application.Current.Properties.Contains("CurrentUser"))
                {
                    Application.Current.Properties.Remove("CurrentUser");
                }

                // Xóa session
                SessionService.DeleteSession();

                System.Diagnostics.Debug.WriteLine("User logged out, session deleted");

                // Chuyển về trang đăng nhập
                var loginView = new LoginView();
                loginView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                loginView.Show();

                // Đóng tất cả các cửa sổ khác
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != loginView)
                    {
                        window.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đăng xuất: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void CheckSessionTimeout()
        {
            try
            {
                if (!SessionService.IsSessionValid())
                {
                    System.Diagnostics.Debug.WriteLine("Session expired, logging out...");
                    MessageBox.Show("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                        "Hết phiên", MessageBoxButton.OK, MessageBoxImage.Information);
                    Logout();
                }
                else
                {
                    // Cập nhật hoạt động
                    SessionService.UpdateSessionActivity();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking session timeout: {ex.Message}");
            }
        }
    }
}