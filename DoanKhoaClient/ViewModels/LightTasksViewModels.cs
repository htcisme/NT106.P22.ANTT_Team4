using System.Windows;
using DoanKhoaClient.Views;

namespace DoanKhoaClient.ViewModels
{
    public class LightTasksViewModels
    {
        // Xử lý sự kiện khi bấm vào iDark
        public void HandleDarkModeClick()
        {
            // Mở cửa sổ DarkTasksView
            var darkTasksView = new DarkTasksView();
            darkTasksView.Show();

            // Đóng cửa sổ hiện tại
            Application.Current.Windows[0]?.Close();
        }

        // Xử lý sự kiện khi bấm vào Notifications
        public void HandleNotificationsClick()
        {
            // Hiển thị hộp thoại thông báo
            MessageBox.Show("Thông báo mới nhất:\n- Công việc A đã hoàn thành.\n- Công việc B đang chờ xử lý.", 
                            "Thông báo", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
        }
    }
}