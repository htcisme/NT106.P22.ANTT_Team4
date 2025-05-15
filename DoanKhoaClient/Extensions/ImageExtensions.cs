using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DoanKhoaClient.Extensions
{
    public static class ImageExtensions
    {
        public static void SetupAsUserAvatar(this Image image)
        {
            // Tạo context menu nếu chưa có
            if (image.ContextMenu == null)
            {
                image.ContextMenu = new ContextMenu();

                // Thêm menu item đăng xuất
                var logoutMenuItem = new MenuItem { Header = "Đăng xuất" };
                logoutMenuItem.Click += (s, e) =>
                {
                    MessageBoxResult result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?",
                        "Xác nhận đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        AuthHelper.Logout();
                    }
                };

                // Thêm menu item xem profile (có thể bổ sung sau)
                var profileMenuItem = new MenuItem { Header = "Xem hồ sơ" };

                // Thêm các menu items vào context menu
                image.ContextMenu.Items.Add(profileMenuItem);
                image.ContextMenu.Items.Add(new Separator());
                image.ContextMenu.Items.Add(logoutMenuItem);
            }

            // Đặt thuộc tính cursor
            image.Cursor = Cursors.Hand;

            // Thêm sự kiện khi click vào avatar
            image.MouseLeftButtonDown += (s, e) => image.ContextMenu.IsOpen = true;
        }
    }
}