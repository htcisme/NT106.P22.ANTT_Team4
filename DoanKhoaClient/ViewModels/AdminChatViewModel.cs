using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Views;

namespace DoanKhoaClient.ViewModels
{
    public class AdminChatViewModel : INotifyPropertyChanged
    {
        // Properties with INotifyPropertyChanged implementation
        private ObservableCollection<Conversation> _conversations;
        public ObservableCollection<Conversation> Conversations
        {
            get => _conversations;
            set
            {
                _conversations = value;
                OnPropertyChanged();
            }
        }

        private Conversation _selectedConversation;
        public Conversation SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Message> _spamMessages;
        public ObservableCollection<Message> SpamMessages
        {
            get => _spamMessages;
            set
            {
                _spamMessages = value;
                OnPropertyChanged();
            }
        }

        private bool _autoFilterSpam = true;
        public bool AutoFilterSpam
        {
            get => _autoFilterSpam;
            set
            {
                _autoFilterSpam = value;
                OnPropertyChanged();
            }
        }

        private bool _notifyOnSpamDetection = true;
        public bool NotifyOnSpamDetection
        {
            get => _notifyOnSpamDetection;
            set
            {
                _notifyOnSpamDetection = value;
                OnPropertyChanged();
            }
        }

        private double _spamFilterLevel = 5;
        public double SpamFilterLevel
        {
            get => _spamFilterLevel;
            set
            {
                _spamFilterLevel = value;
                OnPropertyChanged();
            }
        }

        // Navigation Commands
        public ICommand NavigateToHomeCommand { get; private set; }
        public ICommand NavigateToChatCommand { get; private set; }
        public ICommand NavigateToActivitiesCommand { get; private set; }
        public ICommand NavigateToMembersCommand { get; private set; }
        public ICommand NavigateToTasksCommand { get; private set; }
        public ICommand NavigateToAdminCommand { get; private set; }

        // Action Commands
        public ICommand CreateGroupCommand { get; private set; }
        public ICommand SearchUsersCommand { get; private set; }
        public ICommand DeleteSpamCommand { get; private set; }
        public ICommand RestoreMessageCommand { get; private set; }
        public ICommand SaveSettingsCommand { get; private set; }

        public AdminChatViewModel()
        {
            // Initialize commands using RelayCommand from Helpers
            // Replace the command initialization in constructor with:

            // Initialize commands using RelayCommand from Helpers
            NavigateToHomeCommand = new RelayCommand(_ => NavigateToHome());
            NavigateToChatCommand = new RelayCommand(_ => NavigateToChat());
            NavigateToActivitiesCommand = new RelayCommand(_ => NavigateToActivities());
            NavigateToMembersCommand = new RelayCommand(_ => NavigateToMembers());
            NavigateToTasksCommand = new RelayCommand(_ => NavigateToTasks());
            NavigateToAdminCommand = new RelayCommand(_ => NavigateToAdmin());

            CreateGroupCommand = new RelayCommand(_ => CreateGroup());
            SearchUsersCommand = new RelayCommand(_ => SearchUsers());
            DeleteSpamCommand = new RelayCommand(param => DeleteSpam(param as Message));
            RestoreMessageCommand = new RelayCommand(param => RestoreMessage(param as Message));
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());

            // Initialize data
            LoadData();
        }

        private void LoadData()
        {
            // Load conversations and spam messages from your data source
            Conversations = new ObservableCollection<Conversation>();
            SpamMessages = new ObservableCollection<Message>();

            // Dummy data for example
            Conversations.Add(new Conversation { Id = "1", Title = "Nhóm 1", LastActivity = DateTime.Now });
            Conversations.Add(new Conversation { Id = "2", Title = "Nhóm 2", LastActivity = DateTime.Now.AddMinutes(-30) });

            SpamMessages.Add(new Message { Id = "1", SenderId = "user1", SenderName = "Nguyen Van A", Content = "Spam message content 1", Timestamp = DateTime.Now });
            SpamMessages.Add(new Message { Id = "2", SenderId = "user2", SenderName = "Tran Van B", Content = "Spam message content 2", Timestamp = DateTime.Now });
        }

        // Navigation methods
        private void NavigateToHome()
        {
            var homeView = new HomePageView();
            homeView.Show();
            CloseCurrentWindow();
        }

        private void NavigateToChat()
        {
            // Already on chat view
        }

        private void NavigateToActivities()
        {
            var activitiesView = new AdminActivitiesView();
            activitiesView.Show();
            CloseCurrentWindow();
        }

        private void NavigateToMembers()
        {
            var membersView = new AdminMembersView();
            membersView.Show();
            CloseCurrentWindow();
        }

        private void NavigateToTasks()
        {
            var tasksView = new AdminTasksView(); // Fixed class name from AdminTaskView to AdminTasksView
            tasksView.Show();
            CloseCurrentWindow();
        }

        private void NavigateToAdmin()
        {
            // Already in admin section
        }

        // Helper method to close current window
        private void CloseCurrentWindow()
        {
            // Find and close the current AdminChatView window
            foreach (Window window in Application.Current.Windows)
            {
                if (window is AdminChatView)
                {
                    window.Close();
                    break;
                }
            }
        }

        // Action methods
        private void CreateGroup()
        {
            MessageBox.Show("Chức năng tạo nhóm mới");
        }

        private void SearchUsers()
        {
            MessageBox.Show("Chức năng tìm kiếm người dùng");
        }

        private void DeleteSpam(Message message)
        {
            if (message != null)
            {
                SpamMessages.Remove(message);
                MessageBox.Show($"Đã xóa tin nhắn spam từ {message.SenderName}");
            }
        }

        private void RestoreMessage(Message message)
        {
            if (message != null)
            {
                SpamMessages.Remove(message);
                MessageBox.Show($"Đã khôi phục tin nhắn từ {message.SenderName}");
            }
        }

        private void SaveSettings()
        {
            MessageBox.Show("Đã lưu cài đặt lọc spam");
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}