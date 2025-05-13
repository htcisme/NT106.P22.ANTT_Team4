using DoanKhoaClient.Models;
using DoanKhoaClient.Views;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace DoanKhoaClient.Helpers
{
    public static class NavigationHelper
    {
        // Phương thức điều hướng chung với hiệu ứng
        public static async Task NavigateTo(Window currentWindow, Window newWindow, object fadeOutElement = null)
        {
            // Truyền thông tin người dùng hiện tại (nếu có)
            if (Application.Current.Properties.Contains("CurrentUser") &&
                Application.Current.Properties["CurrentUser"] is User currentUser)
            {
                // Đặt vị trí cửa sổ mới giống với cửa sổ hiện tại
                newWindow.Left = currentWindow.Left;
                newWindow.Top = currentWindow.Top;
                newWindow.Width = currentWindow.Width;
                newWindow.Height = currentWindow.Height;
                newWindow.WindowState = currentWindow.WindowState;
            }

            // Tạo hiệu ứng fade out (nếu được chỉ định)
            if (fadeOutElement != null && fadeOutElement is FrameworkElement element)
            {
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                element.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                // Chờ animation hoàn thành
                await Task.Delay(300);
            }

            // Mở cửa sổ mới và đóng cửa sổ hiện tại
            newWindow.Show();
            currentWindow.Close();
        }

        // Các phương thức điều hướng cụ thể
        public static async Task NavigateToHome(Window currentWindow, object fadeOutElement = null)
        {
            await NavigateTo(currentWindow, new HomePageView(), fadeOutElement);
        }

        public static async Task NavigateToChat(Window currentWindow, object fadeOutElement = null)
        {
            await NavigateTo(currentWindow, new UserChatView(), fadeOutElement);
        }

        public static async Task NavigateToActivities(Window currentWindow, object fadeOutElement = null)
        {
            await NavigateTo(currentWindow, new ActivitiesView(), fadeOutElement);
        }

        public static async Task NavigateToMembers(Window currentWindow, object fadeOutElement = null)
        {
            await NavigateTo(currentWindow, new MembersView(), fadeOutElement);
        }

        public static async Task NavigateToTasks(Window currentWindow, object fadeOutElement = null)
        {
            await NavigateTo(currentWindow, new TasksView(), fadeOutElement);
        }

        // Bổ sung vào lớp NavigationHelper
        public static void TransferUserData(Window source, Window destination)
        {
            // Nếu window đích có DataContext và window nguồn cũng có
            if (destination.DataContext != null && source.DataContext != null)
            {
                // Kiểm tra và truyền thông tin User nếu có
                var sourceViewModel = source.DataContext;
                var destViewModel = destination.DataContext;

                // Truyền CurrentUser nếu cả hai ViewModel có thuộc tính này
                var sourceCurrentUserProperty = sourceViewModel.GetType().GetProperty("CurrentUser");
                var destCurrentUserProperty = destViewModel.GetType().GetProperty("CurrentUser");

                if (sourceCurrentUserProperty != null && destCurrentUserProperty != null)
                {
                    var currentUser = sourceCurrentUserProperty.GetValue(sourceViewModel);
                    if (currentUser != null)
                    {
                        destCurrentUserProperty.SetValue(destViewModel, currentUser);
                    }
                }
            }

            // Sử dụng Application.Current.Properties cho trao đổi dữ liệu toàn cầu
            if (Application.Current.Properties.Contains("CurrentUser"))
            {
                var currentUser = Application.Current.Properties["CurrentUser"];
                // Có thể sử dụng để thiết lập dữ liệu người dùng cho tất cả các trang
            }
        }

    }
}