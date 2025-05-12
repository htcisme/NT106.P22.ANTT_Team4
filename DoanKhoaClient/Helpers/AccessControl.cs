using DoanKhoaClient.Models;
using System.Windows;
using DoanKhoaClient.Views;
using System.Windows.Input;
using DoanKhoaClient.Services;
using System.Windows.Controls;
using System.Windows.Media;
using DoanKhoaClient.ViewModels;

namespace DoanKhoaClient.Helpers
{
    public static class AccessControl
    {
        public static bool IsAdmin()
        {
            if (Application.Current.Properties.Contains("CurrentUser") &&
                Application.Current.Properties["CurrentUser"] is User currentUser)
            {
                return currentUser.Role == UserRole.Admin;
            }
            return false;
        }

        public static void CheckAdminAccess(Window currentWindow)
        {
            if (!IsAdmin())
            {
                MessageBox.Show("Bạn không có quyền truy cập trang này.", "Quyền truy cập bị từ chối",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                // Chuyển hướng về dashboard người dùng thông thường
                var userView = new UserChatView();
                userView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                userView.Show();

                // Đóng cửa sổ hiện tại
                currentWindow.Close();
            }
        }
    }
}