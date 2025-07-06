using System.Windows;
using DoanKhoaClient.Services;

namespace DoanKhoaClient
{
    public partial class App : Application
    {
        private TaskReminderService _reminderService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // THÊM: Khởi tạo automatic reminder service
            try
            {
                var taskService = new TaskService();
                var emailService = new EmailService();
                _reminderService = new TaskReminderService(taskService, emailService);

                // Bắt đầu automatic reminders
                _reminderService.StartReminderService();

                System.Diagnostics.Debug.WriteLine("✅ Automatic Task Reminder Service started successfully!");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to start Reminder Service: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dừng reminder service khi app thoát
            try
            {
                _reminderService?.StopReminderService();
                System.Diagnostics.Debug.WriteLine("🛑 Task Reminder Service stopped.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error stopping Reminder Service: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}