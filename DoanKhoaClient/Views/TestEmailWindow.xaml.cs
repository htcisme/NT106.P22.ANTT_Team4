using System;
using System.Windows;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class TestEmailWindow : Window
    {
        private readonly TaskService _taskService;

        public TestEmailWindow()
        {
            InitializeComponent();
            _taskService = new TaskService();
        }

        private async void SendTestReminderButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập email", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = await _taskService.SendTestEmailAsync(
                    EmailTextBox.Text.Trim(),
                    NameTextBox.Text.Trim() ?? "Test User",
                    "reminder"
                );

                if (result)
                {
                    MessageBox.Show("Email nhắc nhở test đã được gửi thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Gửi email test thất bại!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SendTestOverdueButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập email", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = await _taskService.SendTestEmailAsync(
                    EmailTextBox.Text.Trim(),
                    NameTextBox.Text.Trim() ?? "Test User",
                    "overdue"
                );

                if (result)
                {
                    MessageBox.Show("Email quá hạn test đã được gửi thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Gửi email test thất bại!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}