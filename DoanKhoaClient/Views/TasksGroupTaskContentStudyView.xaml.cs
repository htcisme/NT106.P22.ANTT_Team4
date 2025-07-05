using System;
using System.Windows;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class TasksGroupTaskContentStudyView : Window
    {
        private readonly TaskSession _session;
        private readonly TaskService _taskService;

        public TasksGroupTaskContentStudyView(TaskSession session)
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(GroupTask_Content_Study_Background);
            _session = session;
            _taskService = new TaskService();
            LoadSessionData();
        }

        private void LoadSessionData()
        {
            if (_session != null)
            {
                // Cập nhật thông tin session
                GroupTask_Study_lbManagerStudyTeam.Content = _session.ManagerName;

                // Cập nhật tiêu đề dựa trên session name
                GroupTask_Study_lbHeadTasks1.Content = _session.Name;

                // Cập nhật thông tin thời gian
                if (_session.CreatedAt != default)
                {
                    GroupTask_Study_lbDetailsOpenTime.Content = _session.CreatedAt.ToString("dddd, d MMMM yyyy, h:mm tt");
                }

                if (_session.UpdatedAt != default)
                {
                    GroupTask_Study_lbDetailsCloseTime.Content = _session.UpdatedAt.ToString("dddd, d MMMM yyyy, h:mm tt");
                }

                // Cập nhật mô tả học tập
                GroupTask_Study_lbDescription.Content = GetStudyDescription(_session);
            }
        }

        public void RefreshContent()
        {
            LoadSessionData();
        }

        private string GetStudyDescription(TaskSession session)
        {
            return $"Phiên làm việc học tập: {session.Name}\n" +
                   $"Quản lý bởi: {session.ManagerName}\n" +
                   $"Tạo ngày: {session.CreatedAt:dd/MM/yyyy}\n" +
                   $"Cập nhật: {session.UpdatedAt:dd/MM/yyyy}";
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(GroupTask_Content_Study_Background);
        }

        // Navigation handlers
        private void SidebarHomeButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var homeView = new HomePageView();
            homeView.Show();
            this.Close();
        }

        private void SidebarChatButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var chatView = new UserChatView();
            chatView.Show();
            this.Close();
        }

        private void SidebarTasksButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var tasksView = new TasksView();
            tasksView.Show();
            this.Close();
        }

        private void SidebarActivitiesButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var activitiesView = new ActivitiesView();
            activitiesView.Show();
            this.Close();
        }

        private void SidebarMembersButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var membersView = new HomePageView();
            membersView.Show();
            this.Close();
        }
    }
}