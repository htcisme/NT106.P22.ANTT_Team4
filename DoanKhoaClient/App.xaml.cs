using System;
using System.Windows;
using System.Windows.Threading;
using DoanKhoaClient.Helpers;

namespace DoanKhoaClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DispatcherTimer _sessionTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Khởi động timer để kiểm tra session timeout
            StartSessionTimer();
        }

        private void StartSessionTimer()
        {
            _sessionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // Kiểm tra mỗi phút
            };
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();
        }

        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            // Kiểm tra session timeout
            AuthHelper.CheckSessionTimeout();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _sessionTimer?.Stop();
            base.OnExit(e);
        }
    }
}

