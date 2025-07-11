using System.Windows;
using System.Windows.Controls;
using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Extensions;

namespace DoanKhoaClient.Views
{
    public partial class AdminActivityDetailView : Window
    {
        private AdminActivityDetailViewModel _viewModel;

        public AdminActivityDetailView(Activity activity)
        {
            InitializeComponent();

            // Kiểm tra quyền truy cập admin
            AccessControl.CheckAdminAccess(this);

            // Khởi tạo ViewModel
            _viewModel = new AdminActivityDetailViewModel(activity);
            DataContext = _viewModel;

            // Setup auto-refresh checkbox
            AutoRefreshCheckBox.IsChecked = _viewModel.AutoRefreshEnabled;

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_ActivityDetail_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Cleanup ViewModel trước khi đóng
            _viewModel?.Cleanup();

            // Quay lại trang AdminActivitiesView
            var adminActivitiesView = new AdminActivitiesView();
            adminActivitiesView.Show();
            this.Close();
        }

        #region Auto-refresh Handlers

        private void AutoRefreshCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.AutoRefreshEnabled = true;
            }
        }

        private void AutoRefreshCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.AutoRefreshEnabled = false;
            }
        }

        #endregion

        #region Tab Navigation

        private void OverviewTab_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab("Overview");
        }

        private void ParticipantsTab_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab("Participants");
            _viewModel.LoadParticipantsCommand?.Execute(null);
        }

        private void LikesTab_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab("Likes");
            _viewModel.LoadLikedUsersCommand?.Execute(null);
        }

        private void CommentsTab_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab("Comments");
            _viewModel.LoadCommentsCommand?.Execute(null);
        }

        private void SwitchTab(string tabName)
        {
            // Reset all tab buttons
            OverviewTab.Tag = null;
            ParticipantsTab.Tag = null;
            LikesTab.Tag = null;
            CommentsTab.Tag = null;

            // Hide all content panels
            OverviewContent.Visibility = Visibility.Collapsed;
            ParticipantsContent.Visibility = Visibility.Collapsed;
            LikesContent.Visibility = Visibility.Collapsed;
            CommentsContent.Visibility = Visibility.Collapsed;

            // Show selected tab and content
            switch (tabName)
            {
                case "Overview":
                    OverviewTab.Tag = "Active";
                    OverviewContent.Visibility = Visibility.Visible;
                    break;
                case "Participants":
                    ParticipantsTab.Tag = "Active";
                    ParticipantsContent.Visibility = Visibility.Visible;
                    break;
                case "Likes":
                    LikesTab.Tag = "Active";
                    LikesContent.Visibility = Visibility.Visible;
                    break;
                case "Comments":
                    CommentsTab.Tag = "Active";
                    CommentsContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        // Clean up when window is closed
        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel?.Cleanup();
            base.OnClosed(e);
        }
    }
}