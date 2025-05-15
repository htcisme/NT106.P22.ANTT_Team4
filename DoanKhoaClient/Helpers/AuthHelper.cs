using System;
using System.IO;
using System.Windows;

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

                // Xóa token hoặc dữ liệu session nếu có
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DoanKhoaClient");
                string sessionFilePath = Path.Combine(appDataPath, "session.dat");

                if (File.Exists(sessionFilePath))
                {
                    File.Delete(sessionFilePath);
                }

                // Chuyển về trang đăng nhập
                var loginView = new Views.LoginView();
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
    }
}