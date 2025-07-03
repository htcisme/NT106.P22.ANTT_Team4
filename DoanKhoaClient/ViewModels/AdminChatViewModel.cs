// Cập nhật hoặc tạo mới: AdminChatViewModel.cs
using DoanKhoaClient.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MongoDB.Bson;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DoanKhoaClient.Views;
using DoanKhoaClient.Helpers;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DoanKhoaClient.Properties;
using DoanKhoaClient.Services;
namespace DoanKhoaClient.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class AdminChatViewModel : BaseViewModel
    {
        private readonly HttpClient _httpClient;
        private HubConnection _hubConnection;
        private string _currentMessage = string.Empty;
        private User _currentUser;
        private Conversation _selectedConversation;
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>();
        private ObservableCollection<Conversation> _conversations = new ObservableCollection<Conversation>();
        private ObservableCollection<Message> _spamMessages = new ObservableCollection<Message>();
        private string _searchText = string.Empty;
        private bool _isConnected;
        private bool _autoFilterSpam = true;
        private bool _notifyOnSpamDetection = true;
        private int _spamFilterLevel = 5;

        // Properties
        public string CurrentMessage
        {
            get => _currentMessage;
            set
            {
                _currentMessage = value;
                OnPropertyChanged();
                (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public Conversation SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                OnPropertyChanged();
                if (_selectedConversation != null)
                {
                    LoadMessages(_selectedConversation.Id);
                }
            }
        }

        public ObservableCollection<Message> Messages
        {
            get => _messages;
            set
            {
                _messages = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Conversation> Conversations
        {
            get => _conversations;
            set
            {
                _conversations = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Message> SpamMessages
        {
            get => _spamMessages;
            set
            {
                _spamMessages = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                SearchConversations();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public bool AutoFilterSpam
        {
            get => _autoFilterSpam;
            set
            {
                _autoFilterSpam = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyOnSpamDetection
        {
            get => _notifyOnSpamDetection;
            set
            {
                _notifyOnSpamDetection = value;
                OnPropertyChanged();
            }
        }

        public int SpamFilterLevel
        {
            get => _spamFilterLevel;
            set
            {
                _spamFilterLevel = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SendMessageCommand { get; private set; }
        public ICommand SearchUsersCommand { get; private set; }
        public ICommand CreateGroupCommand { get; private set; }
        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand DownloadFileCommand { get; private set; }
        public ICommand DeleteMessageCommand { get; private set; }
        public ICommand MarkAsSpamCommand { get; private set; }
        public ICommand MarkAsNotSpamCommand { get; private set; }

        public AdminChatViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };

            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
            SearchUsersCommand = new RelayCommand(SearchUsers);
            CreateGroupCommand = new RelayCommand(CreateGroup);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            DownloadFileCommand = new RelayCommand(DownloadFile);
            DeleteMessageCommand = new RelayCommand(DeleteMessage);
            MarkAsSpamCommand = new RelayCommand(MarkAsSpam);
            MarkAsNotSpamCommand = new RelayCommand(MarkAsNotSpam);
            AutoFilterSpam = Settings.Default.AutoFilterSpam;
            NotifyOnSpamDetection = Settings.Default.NotifyOnSpamDetection;
            SpamFilterLevel = Settings.Default.SpamFilterLevel;

            // Tải dữ liệu
            LoadData();

            // Kết nối SignalR
            ConnectToHub();
        }

        private async void LoadData()
        {
            try
            {
                // Tải thông tin người dùng hiện tại (admin)
                if (App.Current.Properties.Contains("CurrentUser"))
                {
                    CurrentUser = (User)App.Current.Properties["CurrentUser"];

                    // Kiểm tra nếu là admin
                    if (CurrentUser.Role != UserRole.Admin)
                    {
                        MessageBox.Show("Bạn không có quyền truy cập vào khu vực quản trị!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Tải danh sách cuộc trò chuyện
                    await LoadConversations();
                    await EnsureAdminChatExists();
                    // Tải danh sách tin nhắn spam
                    await LoadSpamMessages();
                }
                else
                {
                    MessageBox.Show("Vui lòng đăng nhập để sử dụng tính năng này!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ConnectToHub()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5299/chatHub")
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<Message>("ReceiveMessage", (message) =>
                {
                    try
                    {
                        // Kiểm tra nếu là tin nhắn đến DoanKhoaMMT
                        if (message.SenderId == "DoanKhoaMMT" || message.SenderId == CurrentUser?.Id)
                        {
                            return; // Không cần xử lý nếu là tin nhắn từ hệ thống hoặc chính mình
                        }

                        // Xử lý tin nhắn ở đây (có thể là check spam)
                        var isSpam = CheckSpam(message);
                        if (isSpam && AutoFilterSpam)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                SpamMessages.Add(message);
                                if (NotifyOnSpamDetection)
                                {
                                    // Hiển thị notification
                                    var notification = new NotificationWindow($"Phát hiện tin nhắn spam từ {message.SenderName}");
                                    notification.Show();
                                }
                            });
                        }
                        else if (message.ConversationId == _selectedConversation?.Id)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Messages.Add(message);
                                // Auto-scroll xuống tin nhắn mới
                                ScrollToBottom?.Invoke();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in ReceiveMessage: {ex.Message}");
                    }
                });

                _hubConnection.On<string, string>("MessageUpdated", (messageId, newContent) =>
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Cập nhật tin nhắn trong danh sách hiện tại
                            var message = Messages.FirstOrDefault(m => m.Id == messageId);
                            if (message != null)
                            {
                                message.Content = newContent;
                                // Refresh
                                var index = Messages.IndexOf(message);
                                Messages.Remove(message);
                                Messages.Insert(index, message);
                            }

                            // Cập nhật trong danh sách spam nếu có
                            var spamMessage = SpamMessages.FirstOrDefault(m => m.Id == messageId);
                            if (spamMessage != null)
                            {
                                spamMessage.Content = newContent;
                                // Refresh
                                var index = SpamMessages.IndexOf(spamMessage);
                                SpamMessages.Remove(spamMessage);
                                SpamMessages.Insert(index, spamMessage);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error handling MessageUpdated: {ex.Message}");
                    }
                });

                _hubConnection.On<string>("MessageDeleted", (messageId) =>
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Xóa tin nhắn khỏi danh sách hiện tại
                            var message = Messages.FirstOrDefault(m => m.Id == messageId);
                            if (message != null)
                            {
                                Messages.Remove(message);
                            }

                            // Xóa khỏi danh sách spam nếu có
                            var spamMessage = SpamMessages.FirstOrDefault(m => m.Id == messageId);
                            if (spamMessage != null)
                            {
                                SpamMessages.Remove(spamMessage);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error handling MessageDeleted: {ex.Message}");
                    }
                });

                await _hubConnection.StartAsync();
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                MessageBox.Show($"Không thể kết nối đến máy chủ chat: {ex.Message}", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckSpam(Message message)
        {
            // Logic kiểm tra spam đơn giản dựa trên filter level
            if (string.IsNullOrEmpty(message.Content))
                return false;

            string content = message.Content.ToLower();

            // Các từ khóa spam cơ bản
            string[] spamKeywords = { "casino", "viagra", "lottery", "prize", "won money", "click here", "free money", "earn money fast", "investment opportunity" };

            // Đếm số lượng từ khóa spam xuất hiện
            int spamCount = spamKeywords.Count(keyword => content.Contains(keyword));

            // Tùy chỉnh ngưỡng dựa trên SpamFilterLevel (1-10)
            int threshold = 11 - SpamFilterLevel; // Ngược với slider: level 10 = threshold 1, level 1 = threshold 10

            return spamCount >= threshold;
        }

        // Event để scroll xuống tin nhắn mới
        public event Action ScrollToBottom;

        private async Task LoadConversations()
        {
            try
            {
                // Tải tất cả cuộc trò chuyện có người dùng "DoanKhoaMMT"
                var response = await _httpClient.GetAsync("conversations/special/doankhoa");
                if (response.IsSuccessStatusCode)
                {
                    var conversations = await response.Content.ReadFromJsonAsync<List<Conversation>>();
                    if (conversations != null)
                    {
                        Conversations = new ObservableCollection<Conversation>(conversations);

                        // Chọn cuộc trò chuyện đầu tiên nếu có
                        if (Conversations.Count > 0 && SelectedConversation == null)
                        {
                            SelectedConversation = Conversations[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải cuộc trò chuyện: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMessages(string conversationId)
        {
            try
            {
                // Tham gia group SignalR
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("JoinGroup", conversationId);
                }

                // Tải tin nhắn
                var response = await _httpClient.GetAsync($"messages/conversation/{conversationId}");
                if (response.IsSuccessStatusCode)
                {
                    var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
                    if (messages != null)
                    {
                        Messages = new ObservableCollection<Message>(messages);

                        // Scroll xuống tin nhắn cuối cùng
                        ScrollToBottom?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadSpamMessages()
        {
            try
            {
                // API để lấy các tin nhắn bị đánh dấu là spam
                var response = await _httpClient.GetAsync("messages/spam");
                if (response.IsSuccessStatusCode)
                {
                    var spamMessages = await response.Content.ReadFromJsonAsync<List<Message>>();
                    if (spamMessages != null)
                    {
                        SpamMessages = new ObservableCollection<Message>(spamMessages);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải tin nhắn spam: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSendMessage(object parameter)
        {
            return !string.IsNullOrWhiteSpace(CurrentMessage) && SelectedConversation != null;
        }

        private async void SendMessage(object parameter)
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage) || SelectedConversation == null)
                return;

            try
            {
                // Admin gửi tin nhắn dưới tên DoanKhoaMMT
                var message = new Message
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ConversationId = SelectedConversation.Id,
                    SenderId = "DoanKhoaMMT", // Gửi dưới tên hệ thống
                    SenderName = "Đoàn Khoa MMT&TT",
                    Content = CurrentMessage,
                    Timestamp = DateTime.Now,
                    Type = MessageType.Text
                };

                // Thêm tin nhắn vào UI trước
                Messages.Add(message);

                // Xóa nội dung tin nhắn hiện tại
                CurrentMessage = string.Empty;

                // Scroll xuống tin nhắn mới
                ScrollToBottom?.Invoke();

                // Gửi tin nhắn qua SignalR
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("SendMessageAsSystem",
                        SelectedConversation.Id,
                        "DoanKhoaMMT",
                        "Đoàn Khoa MMT&TT",
                        message.Content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchConversations()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Tải lại danh sách đầy đủ
                LoadConversations();
            }
            else
            {
                // Lọc danh sách hiện tại
                var filteredList = Conversations
                    .Where(c => c.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               c.ParticipantIds.Any(id => id.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                Conversations = new ObservableCollection<Conversation>(filteredList);
            }
        }

        // Tìm phương thức SearchUsers và sửa như sau
        private void SearchUsers(object parameter)
        {
            // Hiển thị dialog để tìm kiếm user
            var searchDialog = new FriendSearchDialog(CurrentUser); // Thêm tham số CurrentUser
            if (searchDialog.ShowDialog() == true && searchDialog.SelectedUser != null)
            {
                var selectedUser = searchDialog.SelectedUser;

                // Tìm hoặc tạo cuộc trò chuyện với user này
                FindOrCreateConversation(selectedUser.Id);
            }
        }

        // Thay đổi phương thức SaveSettings

        private async void FindOrCreateConversation(string userId)
        {
            try
            {
                // Tìm cuộc trò chuyện hiện có
                var existingConversation = Conversations.FirstOrDefault(c =>
                    c.ParticipantIds.Contains(userId) &&
                    c.ParticipantIds.Count == 2 &&
                    c.ParticipantIds.Contains(CurrentUser.Id));

                if (existingConversation != null)
                {
                    // Nếu đã tồn tại, chọn nó
                    SelectedConversation = existingConversation;
                }
                else
                {
                    // Tạo cuộc trò chuyện mới
                    var conversation = new Conversation
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Title = "", // Không cần title cho chat riêng tư
                        CreatorId = CurrentUser.Id,
                        ParticipantIds = new List<string> { CurrentUser.Id, userId },
                        ParticipantNames = new Dictionary<string, string>(),
                        CreatedAt = DateTime.UtcNow,
                        LastMessageTimestamp = DateTime.UtcNow
                    };

                    // Thêm tên của người tham gia
                    var user = await GetUserById(userId);
                    if (user != null)
                    {
                        conversation.ParticipantNames.Add(CurrentUser.Id, CurrentUser.DisplayName);
                        conversation.ParticipantNames.Add(userId, user.DisplayName);
                    }

                    var response = await _httpClient.PostAsJsonAsync("conversations", conversation);

                    if (response.IsSuccessStatusCode)
                    {
                        var createdConversation = await response.Content.ReadFromJsonAsync<Conversation>();
                        if (createdConversation != null)
                        {
                            // Thêm vào danh sách và chọn
                            Conversations.Add(createdConversation);
                            SelectedConversation = createdConversation;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không thể tạo cuộc trò chuyện. Vui lòng thử lại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm hoặc tạo cuộc trò chuyện: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Thêm phương thức hỗ trợ GetUserById
        private async Task<User> GetUserById(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user: {ex.Message}");
            }
            return null;
        }

        private void CreateGroup(object parameter)
        {
            try
            {
                // Display dialog to create a chat group
                var createGroupDialog = new CreateGroupDialog();
                bool? result = createGroupDialog.ShowDialog();

                if (result == true && createGroupDialog.DialogConfirmed)
                {
                    string groupName = createGroupDialog.GroupName;
                    List<User> selectedUsers = createGroupDialog.SelectedUsers;

                    if (!string.IsNullOrEmpty(groupName) && selectedUsers != null && selectedUsers.Count > 0)
                    {
                        CreateGroupConversation(groupName, selectedUsers);
                    }
                    else
                    {
                        MessageBox.Show("Cần nhập tên nhóm và chọn ít nhất một thành viên",
                            "Thông tin không đầy đủ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo nhóm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Add this method to create a shared admin chat
        private async Task EnsureAdminChatExists()
        {
            try
            {
                // Check if admin chat already exists
                var adminChat = Conversations.FirstOrDefault(c =>
                    c.IsGroup && c.Title == "Admin Chat" && c.IsAdminOnly);

                if (adminChat != null)
                {
                    // Admin chat exists, select it
                    SelectedConversation = adminChat;
                    return;
                }

                // Get all admin users
                var response = await _httpClient.GetAsync("users/admins");
                if (response.IsSuccessStatusCode)
                {
                    var adminUsers = await response.Content.ReadFromJsonAsync<List<User>>();

                    if (adminUsers != null && adminUsers.Count > 0)
                    {
                        // Create the admin group
                        var conversation = new Conversation
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Title = "Admin Chat",
                            CreatorId = CurrentUser.Id,
                            ParticipantIds = adminUsers.Select(u => u.Id).ToList(),
                            ParticipantNames = adminUsers.ToDictionary(u => u.Id, u => u.DisplayName),
                            CreatedAt = DateTime.UtcNow,
                            LastMessageTimestamp = DateTime.UtcNow,
                            IsGroup = true,
                            IsAdminOnly = true
                        };

                        var createResponse = await _httpClient.PostAsJsonAsync("conversations", conversation);
                        if (createResponse.IsSuccessStatusCode)
                        {
                            var createdConversation = await createResponse.Content.ReadFromJsonAsync<Conversation>();
                            if (createdConversation != null)
                            {
                                Conversations.Add(createdConversation);
                                SelectedConversation = createdConversation;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo nhóm Admin: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void CreateGroupConversation(string title, List<User> participants)
        {
            try
            {
                // Thêm người dùng hiện tại vào danh sách nếu chưa có
                if (!participants.Any(u => u.Id == CurrentUser.Id))
                {
                    participants.Add(CurrentUser);
                }

                // Tạo đối tượng Conversation để gửi lên server
                var conversation = new Conversation
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Title = title,
                    CreatorId = CurrentUser.Id,
                    ParticipantIds = participants.Select(u => u.Id).ToList(),
                    ParticipantNames = participants.ToDictionary(u => u.Id, u => u.DisplayName),
                    CreatedAt = DateTime.UtcNow,
                    LastMessageTimestamp = DateTime.UtcNow,
                    IsGroup = true
                };

                // Gửi lên server
                var response = await _httpClient.PostAsJsonAsync("conversations", conversation);

                if (response.IsSuccessStatusCode)
                {
                    var createdConversation = await response.Content.ReadFromJsonAsync<Conversation>();
                    if (createdConversation != null)
                    {
                        // Thêm vào danh sách và chọn
                        Conversations.Add(createdConversation);
                        SelectedConversation = createdConversation;

                        MessageBox.Show($"Đã tạo nhóm chat '{title}' thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Không thể tạo nhóm chat. Vui lòng thử lại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo nhóm chat: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveSettings(object parameter)
        {
            try
            {
                // Lưu cài đặt vào cấu hình - sửa đường dẫn đến Settings
                Settings.Default.AutoFilterSpam = AutoFilterSpam;
                Settings.Default.NotifyOnSpamDetection = NotifyOnSpamDetection;
                Settings.Default.SpamFilterLevel = SpamFilterLevel;
                Settings.Default.Save();

                MessageBox.Show("Đã lưu cài đặt thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void DownloadFile(object parameter)
        {
            if (parameter is Attachment attachment)
            {
                try
                {
                    // Hiện dialog chọn nơi lưu file
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = attachment.FileName,
                        Filter = "All files (*.*)|*.*"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var saveFilePath = saveDialog.FileName;

                        // Tải file từ server
                        using (HttpClient client = new HttpClient())
                        {
                            using (var response = await client.GetAsync(attachment.FileUrl))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
                                    {
                                        await response.Content.CopyToAsync(fileStream);
                                    }

                                    MessageBox.Show($"Đã tải xuống {attachment.FileName} thành công",
                                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show($"Không thể tải file: {response.ReasonPhrase}",
                                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tải file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteMessage(object parameter)
        {
            if (parameter is Message message)
            {
                var result = MessageBox.Show("Bạn có chắc muốn xóa tin nhắn này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"messages/{message.Id}");

                        if (response.IsSuccessStatusCode)
                        {
                            // Xóa tin nhắn khỏi UI
                            if (Messages.Contains(message))
                                Messages.Remove(message);

                            if (SpamMessages.Contains(message))
                                SpamMessages.Remove(message);

                            // Thông báo xóa tin nhắn qua SignalR
                            if (_hubConnection?.State == HubConnectionState.Connected)
                            {
                                await _hubConnection.InvokeAsync("DeleteMessage", message.Id, message.ConversationId);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Không thể xóa tin nhắn. Vui lòng thử lại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void MarkAsSpam(object parameter)
        {
            if (parameter is Message message)
            {
                try
                {
                    // Đánh dấu tin nhắn là spam
                    var response = await _httpClient.PostAsync($"messages/{message.Id}/markSpam", null);

                    if (response.IsSuccessStatusCode)
                    {
                        // Thêm vào danh sách spam và xóa khỏi danh sách tin nhắn thường
                        if (!SpamMessages.Any(m => m.Id == message.Id))
                            SpamMessages.Add(message);

                        if (Messages.Contains(message))
                            Messages.Remove(message);

                        MessageBox.Show("Đã đánh dấu tin nhắn là spam!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi đánh dấu tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void MarkAsNotSpam(object parameter)
        {
            if (parameter is Message message)
            {
                try
                {
                    // Đánh dấu tin nhắn không phải spam
                    var response = await _httpClient.PostAsync($"messages/{message.Id}/unmarkSpam", null);

                    if (response.IsSuccessStatusCode)
                    {
                        // Xóa khỏi danh sách spam
                        if (SpamMessages.Contains(message))
                            SpamMessages.Remove(message);

                        // Thêm vào danh sách tin nhắn thường nếu thuộc cuộc trò chuyện hiện tại
                        if (message.ConversationId == SelectedConversation?.Id)
                            if (!Messages.Any(m => m.Id == message.Id))
                                Messages.Add(message);

                        MessageBox.Show("Đã bỏ đánh dấu tin nhắn là spam!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi bỏ đánh dấu tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}