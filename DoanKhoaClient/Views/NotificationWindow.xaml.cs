using System;
using System.Windows;
using System.Windows.Threading;

namespace DoanKhoaClient.Views
{
    public partial class NotificationWindow : Window
    {
        private DispatcherTimer _timer;

        public NotificationWindow(string message)
        {
            InitializeComponent();

            NotificationText.Text = message;

            // Đặt vị trí ở góc phải dưới màn hình
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;

            // Tạo timer để tự động đóng sau 5 giây
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}