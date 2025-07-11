using System.Windows;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Helpers
{
    public static class AccessControl
    {
        private static bool _isAdmin = false;
        private static string _userId = string.Empty;

        public static void SetCurrentUser(AuthResponse user)
        {
            if (user != null)
            {
                // Debug để kiểm tra giá trị role
                //System.Diagnostics.Debug.WriteLine($"Setting current user: {user.Username}, Role: {user.Role}");

                _isAdmin = user.Role == UserRole.Admin;
                _userId = user.Id;
            }
            else
            {
                _isAdmin = false;
                _userId = string.Empty;
            }
        }
        public static void SetAdminForTesting(bool isAdmin)
        {
            _isAdmin = isAdmin;
        }
        public static bool IsAdmin()
        {
            return _isAdmin;
        }

        public static string GetCurrentUserId()
        {
            return _userId;
        }

        public static void CheckAdminAccess(Window adminWindow)
        {
            if (!IsAdmin())
            {
                MessageBox.Show("Bạn không có quyền truy cập trang này.", "Không có quyền truy cập", MessageBoxButton.OK, MessageBoxImage.Warning);
                var homePage = new DoanKhoaClient.Views.HomePageView();
                homePage.Show();
                adminWindow.Close();
            }
        }
    }
}