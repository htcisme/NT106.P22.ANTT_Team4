// TasksGroupTaskContentDesignView.xaml.cs
using System.Windows;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskContentDesignView : Window
    {
        private readonly TaskSession _session;

        public TasksGroupTaskContentDesignView(TaskSession session)
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Content_Design_Background);
            _session = session;
            LoadSessionData();
        }

        private void LoadSessionData()
        {
            if (_session != null)
            {
                // Cập nhật thông tin session
                GroupTask_Design_lbManagerDesignTeam.Content = _session.ManagerName;

                // Cập nhật tiêu đề dựa trên session name
                GroupTask_Design_lbHeadTasks1.Content = _session.Name;

                // Cập nhật thông tin thời gian (nếu có trong model)
                if (_session.CreatedAt != default)
                {
                    GroupTask_Design_lbDetailsOpenTime.Content = _session.CreatedAt.ToString("dddd, d MMMM yyyy, h:mm tt");
                }

                if (_session.UpdatedAt != default)
                {
                    GroupTask_Design_lbDetailsCloseTime.Content = _session.UpdatedAt.ToString("dddd, d MMMM yyyy, h:mm tt");
                }

                // Cập nhật mô tả (nếu có)
                // GroupTask_Design_lbDescription.Content = _session.Description ?? "Chưa có mô tả chi tiết";
            }
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Content_Design_Background);
        }
    }
}