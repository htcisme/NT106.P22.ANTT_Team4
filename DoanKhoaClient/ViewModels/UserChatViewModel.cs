using DoanKhoaClient.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Views;
using DoanKhoaClient.Services;
using DoanKhoaClient.Helpers;
using MongoDB.Bson;
using DoanKhoaClient.Properties;
using System.Windows.Media.Imaging; // Cho BitmapImage, BitmapCacheOption, BitmapCreateOptions
using System.Windows.Controls; // Cho Image
using System.Windows.Media; // Cho các lớp đồ họa khác
using System.Collections.Generic; // Add this for Dictionary and List
using System.Net.Http.Headers; // Cho MediaTypeHeaderValue
namespace DoanKhoaClient.ViewModels
{
    public class UserChatViewModel : INotifyPropertyChanged
    {
        private Dictionary<string, List<Message>> _conversationMessagesCache = new Dictionary<string, List<Message>>();

        private readonly HttpClient _httpClient;
        private HubConnection _hubConnection;
        private string _currentMessage = string.Empty;
        private User _currentUser;
        private Conversation _selectedConversation;
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>();
        private ObservableCollection<Conversation> _conversations = new ObservableCollection<Conversation>();
        private ObservableCollection<User> _users = new ObservableCollection<User>();
        private string _searchText = string.Empty;
        private bool _isConnected;
        private ObservableCollection<Attachment> _selectedAttachments = new ObservableCollection<Attachment>();
        private bool _isAttachmentsPanelOpen = false;

        // Add search-related properties similar to ActivitiesView
        private ObservableCollection<DoanKhoaClient.Models.Activity> _searchResults = new ObservableCollection<DoanKhoaClient.Models.Activity>();
        private bool _isSearchResultOpen = false;
        private readonly ActivityService _activityService;

        public event PropertyChangedEventHandler PropertyChanged;

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
                if (_selectedConversation != value)
                {
                    // Lưu giá trị cũ để so sánh
                    var oldConversation = _selectedConversation;

                    _selectedConversation = value;

                    // Clear current messages before loading new ones
                    Messages = new ObservableCollection<Message>();

                    OnPropertyChanged();

                    if (_selectedConversation != null)
                    {
                        // Log để debug
                        System.Diagnostics.Debug.WriteLine($"Chuyển sang conversation: {_selectedConversation.Id}");

                        // Rời khỏi nhóm cũ
                        if (oldConversation != null && _hubConnection?.State == HubConnectionState.Connected)
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await _hubConnection.InvokeAsync("LeaveGroup", oldConversation.Id);
                                    System.Diagnostics.Debug.WriteLine($"Đã rời khỏi nhóm: {oldConversation.Id}");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Lỗi khi rời nhóm: {ex.Message}");
                                }
                            });
                        }

                        // Tải tin nhắn
                        Task.Run(async () =>
                        {
                            try
                            {
                                // Thêm bước kiểm tra cache trước khi tải
                                await VerifyCacheIntegrity(_selectedConversation.Id);
                                await LoadMessages(_selectedConversation.Id);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải tin nhắn: {ex.Message}");

                                // Nếu có lỗi, thử tải lại từ server
                                _conversationMessagesCache.Remove(_selectedConversation.Id);
                                await LoadMessages(_selectedConversation.Id);
                            }
                        });
                    }
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

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
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
                // Update search behavior to work like ActivitiesView
                SearchActivities();
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

        public ObservableCollection<Attachment> SelectedAttachments
        {
            get => _selectedAttachments;
            set
            {
                _selectedAttachments = value;
                OnPropertyChanged();
            }
        }

        public bool IsAttachmentsPanelOpen
        {
            get => _isAttachmentsPanelOpen;
            set
            {
                _isAttachmentsPanelOpen = value;
                OnPropertyChanged();
            }
        }

        // Add search-related properties
        public ObservableCollection<DoanKhoaClient.Models.Activity> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearchResultOpen
        {
            get => _isSearchResultOpen;
            set
            {
                _isSearchResultOpen = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SendMessageCommand { get; private set; }
        public ICommand NewConversationCommand { get; private set; }
        public ICommand SelectFileCommand { get; private set; }
        public ICommand SelectImageCommand { get; private set; }
        public ICommand RemoveAttachmentCommand { get; private set; }
        public ICommand CreateGroupCommand { get; private set; }
        public ICommand ShowAttachmentsPanelCommand { get; private set; }
        // Thêm vào phần khai báo command trong UserChatViewModel
        public ICommand EditMessageCommand { get; private set; }
        public ICommand DeleteMessageCommand { get; private set; }
        public ICommand DownloadFileCommand { get; private set; }
        private ICommand _searchFriendsCommand;
        public ICommand SearchFriendsCommand => _searchFriendsCommand ??= new RelayCommand(SearchFriends);

        // Add OpenActivityDetailCommand like in ActivitiesView
        public ICommand OpenActivityDetailCommand { get; private set; }

        // Navigation Commands
        public ICommand NavigateToHomeCommand { get; private set; }
        public ICommand NavigateToChatCommand { get; private set; }
        public ICommand NavigateToActivitiesCommand { get; private set; }
        public ICommand NavigateToMembersCommand { get; private set; }
        public ICommand NavigateToTasksCommand { get; private set; }

        public UserChatViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };
            _activityService = new ActivityService(); // Initialize activity service

            // Initialize search results collection
            SearchResults = new ObservableCollection<DoanKhoaClient.Models.Activity>();

            // Khởi tạo commands
            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
            NewConversationCommand = new RelayCommand(NewConversation);
            SelectFileCommand = new RelayCommand(SelectFile);
            SelectImageCommand = new RelayCommand(SelectImage);
            RemoveAttachmentCommand = new RelayCommand(RemoveAttachment);
            CreateGroupCommand = new RelayCommand(CreateGroup);
            ShowAttachmentsPanelCommand = new RelayCommand(ShowAttachmentsPanel);

            EditMessageCommand = new RelayCommand<Message>(EditMessage, CanEditMessage);
            DeleteMessageCommand = new RelayCommand<Message>(DeleteMessage, CanDeleteMessage);
            DownloadFileCommand = new RelayCommand(DownloadFile);

            // Add OpenActivityDetailCommand
            OpenActivityDetailCommand = new RelayCommand(param => ExecuteOpenActivityDetail(param as DoanKhoaClient.Models.Activity), param => param is DoanKhoaClient.Models.Activity);

            NavigateToHomeCommand = new RelayCommand(NavigateToHome);
            NavigateToChatCommand = new RelayCommand(NavigateToChat);
            NavigateToActivitiesCommand = new RelayCommand(NavigateToActivities);
            NavigateToMembersCommand = new RelayCommand(NavigateToMembers);
            NavigateToTasksCommand = new RelayCommand(NavigateToTasks);

            // Tải dữ liệu
            LoadData();

            // Kết nối SignalR
            ConnectToHub();
        }

        // Add search functionality similar to ActivitiesView
        private async void SearchActivities()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    // Get activities from the service
                    var activities = await _activityService.GetActivitiesAsync();

                    if (activities != null)
                    {
                        var searchResults = activities.Where(a =>
                            a.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            a.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        SearchResults = new ObservableCollection<DoanKhoaClient.Models.Activity>(searchResults);
                        IsSearchResultOpen = true;
                    }
                }
                else
                {
                    SearchResults.Clear();
                    IsSearchResultOpen = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching activities: {ex.Message}");
                SearchResults.Clear();
                IsSearchResultOpen = false;
            }
        }

        // Add method to handle opening activity detail
        private void ExecuteOpenActivityDetail(DoanKhoaClient.Models.Activity activity)
        {
            if (activity == null) return;
            var postView = new ActivitiesPostView(activity);
            postView.Show();
            IsSearchResultOpen = false;
        }

        private bool CanEditMessage(Message message)
        {
            return message != null && message.SenderId == CurrentUser?.Id;
        }

        private bool CanDeleteMessage(Message message)
        {
            return message != null && message.SenderId == CurrentUser?.Id;
        }

        private async void LoadData()
        {
            try
            {
                // Kiểm tra người dùng hiện tại
                if (App.Current.Properties.Contains("CurrentUser"))
                {
                    CurrentUser = (User)App.Current.Properties["CurrentUser"];

                    // Tải cuộc trò chuyện
                    var conversationsResponse = await _httpClient.GetAsync($"conversations/user/{CurrentUser.Id}");
                    if (conversationsResponse.IsSuccessStatusCode)
                    {
                        var conversations = await conversationsResponse.Content.ReadFromJsonAsync<List<Conversation>>();
                        if (conversations != null)
                        {
                            Conversations = new ObservableCollection<Conversation>(conversations);

                            // Hiển thị cuộc trò chuyện đầu tiên nếu có
                            if (Conversations.Count > 0)
                            {
                                SelectedConversation = Conversations[0];
                            }
                        }
                    }

                    // Tải danh sách người dùng
                    var usersResponse = await _httpClient.GetAsync("users");
                    if (usersResponse.IsSuccessStatusCode)
                    {
                        var users = await usersResponse.Content.ReadFromJsonAsync<List<User>>();
                        if (users != null)
                        {
                            Users = new ObservableCollection<User>(users.Where(u => u.Id != CurrentUser.Id));
                        }
                    }

                    // Hiển thị menu Admin nếu là admin
                    if (CurrentUser.Role == UserRole.Admin)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (Application.Current.MainWindow is UserChatView view)
                            {
                                view.SidebarAdminButton.Visibility = Visibility.Visible;
                            }
                        });
                    }
                }
                else
                {
                    // Chuyển về trang đăng nhập nếu chưa đăng nhập
                    NavigateToLogin();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DownloadFile(object parameter)
        {
            if (parameter is Attachment attachment)
            {
                try
                {
                    // Hiện dialog lưu file
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = attachment.FileName,
                        Filter = "All files (*.*)|*.*"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        string saveFilePath = saveDialog.FileName;

                        // Kiểm tra xem file có tồn tại trên server local không
                        string serverFilePath = FilePathService.GetServerFilePath(attachment.FileUrl);

                        if (!string.IsNullOrEmpty(serverFilePath) && File.Exists(serverFilePath))
                        {
                            // Sao chép trực tiếp từ server folder
                            File.Copy(serverFilePath, saveFilePath, true);
                            MessageBox.Show($"Đã tải xuống {attachment.FileName} thành công",
                                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            // Fallback sang HTTP download
                            string fileUrl = FilePathService.GetFullUrl(attachment.FileUrl);

                            using (HttpClient client = new HttpClient())
                            {
                                using (var response = await client.GetAsync(fileUrl))
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tải file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private async void EditMessage(Message message)
        {
            try
            {
                if (message == null) return;

                var editDialog = new EditMessageDialog(message.Content)
                {
                    Owner = Application.Current.MainWindow
                };

                if (editDialog.ShowDialog() == true)
                {
                    string newContent = editDialog.EditedContent;
                    if (!string.IsNullOrWhiteSpace(newContent) && newContent != message.Content)
                    {
                        // SỬA LẠI: Gửi đúng format request với UserId (không phải Userld)
                        var updateRequest = new
                        {
                            Content = newContent,
                            UserId = CurrentUser.Id  // Đảm bảo đúng tên field và có giá trị
                        };

                        System.Diagnostics.Debug.WriteLine($"Sending edit request: Content={newContent}, UserId={CurrentUser.Id}");

                        var response = await _httpClient.PutAsJsonAsync($"messages/{message.Id}", updateRequest);

                        if (response.IsSuccessStatusCode)
                        {
                            // Cập nhật local
                            message.Content = newContent;
                            message.IsEdited = true;

                            // Thông báo qua SignalR để các client khác cập nhật
                            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                            {
                                try
                                {
                                    await _hubConnection.InvokeAsync("UpdateMessage", message.Id, newContent);
                                }
                                catch (Exception signalREx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"SignalR error: {signalREx.Message}");
                                }
                            }

                            MessageBox.Show("Tin nhắn đã được chỉnh sửa thành công!", "Thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"Edit message failed: {response.StatusCode} - {errorContent}");
                            MessageBox.Show($"Không thể chỉnh sửa tin nhắn: {errorContent}", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in EditMessage: {ex.Message}");
                MessageBox.Show($"Lỗi khi chỉnh sửa tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteMessage(Message message)
        {
            try
            {
                if (message == null) return;

                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa tin nhắn này?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // SỬA LẠI: Gửi userId qua query parameter với URL đúng
                    System.Diagnostics.Debug.WriteLine($"Deleting message: {message.Id}, UserId: {CurrentUser.Id}");

                    var response = await _httpClient.DeleteAsync($"messages/{message.Id}?userId={CurrentUser.Id}");

                    if (response.IsSuccessStatusCode)
                    {
                        // Remove from local collection
                        Messages.Remove(message);

                        // Cập nhật cache
                        if (_conversationMessagesCache.ContainsKey(message.ConversationId))
                        {
                            var cachedMessages = _conversationMessagesCache[message.ConversationId];
                            var messageToRemove = cachedMessages.FirstOrDefault(m => m.Id == message.Id);
                            if (messageToRemove != null)
                            {
                                cachedMessages.Remove(messageToRemove);
                            }
                        }

                        MessageBox.Show("Đã xóa tin nhắn thành công!", "Thành công",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Delete message failed: {response.StatusCode} - {errorContent}");
                        MessageBox.Show($"Không thể xóa tin nhắn: {errorContent}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in DeleteMessage: {ex.Message}");
                MessageBox.Show($"Lỗi khi xóa tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task RefreshMessages(string conversationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"messages/conversation/{conversationId}");
                if (response.IsSuccessStatusCode)
                {
                    var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
                    if (messages != null)
                    {

                        System.Diagnostics.Debug.WriteLine($"Loaded {messages.Count} messages");

                        // Tạo một danh sách tin nhắn mới để không ảnh hưởng đến danh sách hiện tại
                        List<Message> processedMessages = new List<Message>();

                        foreach (var message in messages)
                        {
                            // Tạo một bản sao của tin nhắn
                            Message processedMessage = new Message
                            {
                                Id = message.Id,
                                ConversationId = message.ConversationId,
                                SenderId = message.SenderId,
                                SenderName = message.SenderName,
                                Content = message.Content,
                                Timestamp = message.Timestamp,
                                Type = message.Type,
                                Attachments = new List<Attachment>()
                            };

                            // Xử lý các tệp đính kèm
                            if (message.Attachments != null && message.Attachments.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Message {message.Id} has {message.Attachments.Count} attachments");

                                // When processing attachments in RefreshMessages or other methods loading messages
                                foreach (var attachment in message.Attachments)
                                {
                                    // Create processed attachment object
                                    Attachment processedAttachment = new Attachment
                                    {
                                        Id = attachment.Id,
                                        MessageId = string.IsNullOrEmpty(attachment.MessageId) ? message.Id : attachment.MessageId,
                                        FileName = attachment.FileName,
                                        ContentType = attachment.ContentType,
                                        FilePath = attachment.FilePath,
                                        FileSize = attachment.FileSize,
                                        IsImage = attachment.ContentType?.StartsWith("image/") ?? false,
                                        UploadDate = attachment.UploadDate,
                                        UploaderId = attachment.UploaderId,
                                        ThumbnailUrl = attachment.ThumbnailUrl
                                    };

                                    // CRITICAL FIX: Always ensure FileUrl is set
                                    // If no FileUrl but we have FileName, construct a URL
                                    if (string.IsNullOrEmpty(attachment.FileUrl) && !string.IsNullOrEmpty(attachment.FileName))
                                    {
                                        processedAttachment.FileUrl = $"http://localhost:5299/Uploads/{attachment.FileName}";
                                        System.Diagnostics.Debug.WriteLine($"Constructed missing URL from filename: {processedAttachment.FileUrl}");
                                    }
                                    else if (!string.IsNullOrEmpty(attachment.FileUrl))
                                    {
                                        // Use existing URL but ensure it's absolute
                                        if (Uri.IsWellFormedUriString(attachment.FileUrl, UriKind.Absolute))
                                        {
                                            processedAttachment.FileUrl = attachment.FileUrl;
                                        }
                                        else
                                        {
                                            processedAttachment.FileUrl = $"http://localhost:5299/Uploads/{Path.GetFileName(attachment.FileUrl)}";
                                        }
                                    }

                                    // Add the processed attachment to the message
                                    processedMessage.Attachments.Add(processedAttachment);
                                }
                            }

                            processedMessages.Add(processedMessage);

                        }
                        _conversationMessagesCache[conversationId] = processedMessages;

                        // Cập nhật UI nếu đây là conversation hiện tại
                        if (_selectedConversation != null && _selectedConversation.Id == conversationId)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Chỉ cập nhật nếu có tin nhắn mới
                                if (Messages.Count != processedMessages.Count)
                                {
                                    Messages = new ObservableCollection<Message>(processedMessages);
                                    ScrollToLastMessage();
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing messages: {ex.Message}");
            }
        }
        private async Task LoadMessages(string conversationId)
        {
            try
            {
                // Thêm log để debug
                System.Diagnostics.Debug.WriteLine($"[START] LoadMessages cho conversation: {conversationId}");

                // Xóa cache để buộc tải lại từ server
                if (_conversationMessagesCache.ContainsKey(conversationId))
                {
                    _conversationMessagesCache.Remove(conversationId);
                    System.Diagnostics.Debug.WriteLine("Đã xóa cache cũ để tải mới");
                }

                // Tham gia SignalR hub
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("JoinGroup", conversationId);
                    System.Diagnostics.Debug.WriteLine($"Đã tham gia nhóm SignalR: {conversationId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cảnh báo: SignalR không được kết nối");
                    // Thử kết nối lại
                    await ConnectToHub();
                }

                // Tải tin nhắn từ API
                System.Diagnostics.Debug.WriteLine($"Tải tin nhắn từ API: messages/conversation/{conversationId}");
                var response = await _httpClient.GetAsync($"messages/conversation/{conversationId}");

                if (response.IsSuccessStatusCode)
                {
                    var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
                    System.Diagnostics.Debug.WriteLine($"API response success, tin nhắn nhận được: {(messages != null ? messages.Count : 0)}");

                    if (messages != null && messages.Count > 0)
                    {
                        // Xử lý và tạo danh sách tin nhắn đã xử lý
                        List<Message> processedMessages = new List<Message>();

                        foreach (var message in messages)
                        {
                            // Tạo một bản sao của tin nhắn
                            Message processedMessage = new Message
                            {
                                Id = message.Id,
                                ConversationId = message.ConversationId,
                                SenderId = message.SenderId,
                                SenderName = message.SenderName,
                                Content = message.Content,
                                Timestamp = message.Timestamp,
                                Type = message.Type,
                                Attachments = new List<Attachment>()
                            };

                            // Xử lý các tệp đính kèm
                            if (message.Attachments != null && message.Attachments.Count > 0)
                            {
                                foreach (var attachment in message.Attachments)
                                {
                                    // Tạo bản sao của attachment
                                    var processedAttachment = new Attachment
                                    {
                                        Id = attachment.Id,
                                        MessageId = string.IsNullOrEmpty(attachment.MessageId) ? message.Id : attachment.MessageId,
                                        FileName = attachment.FileName,
                                        ContentType = attachment.ContentType,
                                        FilePath = attachment.FilePath,
                                        FileSize = attachment.FileSize,
                                        IsImage = attachment.IsImage,
                                        UploadDate = attachment.UploadDate,
                                        UploaderId = attachment.UploaderId,
                                        ThumbnailUrl = attachment.ThumbnailUrl
                                    };

                                    // Xử lý FileUrl
                                    if (!string.IsNullOrEmpty(attachment.FileUrl))
                                    {
                                        // Use existing URL but ensure it's absolute
                                        if (Uri.IsWellFormedUriString(attachment.FileUrl, UriKind.Absolute))
                                        {
                                            processedAttachment.FileUrl = attachment.FileUrl;
                                            System.Diagnostics.Debug.WriteLine($"Using absolute URL: {processedAttachment.FileUrl}");
                                        }
                                        else if (attachment.FileUrl.StartsWith("/Uploads/"))
                                        {
                                            processedAttachment.FileUrl = $"http://localhost:5299{attachment.FileUrl}";
                                            System.Diagnostics.Debug.WriteLine($"Fixed relative URL with /Uploads/: {processedAttachment.FileUrl}");
                                        }
                                        else
                                        {
                                            string fileName = Path.GetFileName(attachment.FileUrl);
                                            processedAttachment.FileUrl = $"http://localhost:5299/Uploads/{fileName}";
                                            System.Diagnostics.Debug.WriteLine($"Constructed URL from filename: {processedAttachment.FileUrl}");
                                        }
                                    }
                                    else if (!string.IsNullOrEmpty(attachment.FileName))
                                    {
                                        // If FileUrl is empty but we have FileName, construct URL from filename
                                        processedAttachment.FileUrl = $"http://localhost:5299/Uploads/{attachment.FileName}";
                                        System.Diagnostics.Debug.WriteLine($"Created URL from FileName: {processedAttachment.FileUrl}");
                                    }
                                    else if (processedAttachment.IsImage)
                                    {
                                        // Last resort - try using the attachment ID as filename
                                        processedAttachment.FileUrl = $"http://localhost:5299/Uploads/{attachment.Id}";
                                        System.Diagnostics.Debug.WriteLine($"WARNING: Created fallback URL from ID: {processedAttachment.FileUrl}");
                                    }

                                    processedMessage.Attachments.Add(processedAttachment);
                                }
                            }

                            // Thêm tin nhắn đã xử lý vào danh sách
                            processedMessages.Add(processedMessage);
                        }

                        // Lưu vào cache và hiển thị
                        _conversationMessagesCache[conversationId] = processedMessages;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Messages = new ObservableCollection<Message>(processedMessages);
                            ScrollToLastMessage();
                            System.Diagnostics.Debug.WriteLine($"Đã hiển thị {Messages.Count} tin nhắn lên UI");
                        });
                    }
                    else
                    {
                        // Hiển thị thông báo nếu không có tin nhắn
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Messages.Clear();
                            System.Diagnostics.Debug.WriteLine("Không có tin nhắn trong cuộc trò chuyện này");
                        });
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API error: {response.StatusCode} - {errorContent}");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Messages.Clear();
                        MessageBox.Show($"Không thể tải tin nhắn: {response.StatusCode} - {errorContent}",
                                      "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }

                System.Diagnostics.Debug.WriteLine($"[END] LoadMessages");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in LoadMessages: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();
                    MessageBox.Show($"Lỗi tải tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
        private void ScrollToLastMessage()
        {
            // Method để cuộn xuống tin nhắn cuối cùng
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is UserChatView view)
                {
                    view.MessageScrollViewer.ScrollToEnd();
                }
            });
        }
        private async Task ConnectToHub()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5299/chatHub")
                    .WithAutomaticReconnect()
                    .Build();

                // SỬA LẠI: Xử lý tin nhắn nhận được từ người khác
                _hubConnection.On<Message>("ReceiveMessage", (message) =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[REALTIME] Received message from {message.SenderName}: {message.Content}");

                        // XỬ LÝ ATTACHMENTS GIỐNG NHU KHI LOAD TỪ DATABASE
                        if (message.Attachments != null && message.Attachments.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[REALTIME] Processing {message.Attachments.Count} attachments");

                            var processedAttachments = new List<Attachment>();

                            foreach (var attachment in message.Attachments)
                            {
                                var processedAttachment = new Attachment
                                {
                                    Id = attachment.Id,
                                    MessageId = string.IsNullOrEmpty(attachment.MessageId) ? message.Id : attachment.MessageId,
                                    FileName = attachment.FileName,
                                    ContentType = attachment.ContentType,
                                    FilePath = attachment.FilePath,
                                    FileSize = attachment.FileSize,
                                    IsImage = attachment.ContentType?.StartsWith("image/") ?? false,
                                    UploadDate = attachment.UploadDate,
                                    UploaderId = attachment.UploaderId,
                                    ThumbnailUrl = attachment.ThumbnailUrl
                                };

                                // XỬ LÝ FileUrl GIỐNG NHU TRONG LoadMessages
                                if (!string.IsNullOrEmpty(attachment.FileUrl))
                                {
                                    if (Uri.IsWellFormedUriString(attachment.FileUrl, UriKind.Absolute))
                                    {
                                        processedAttachment.FileUrl = attachment.FileUrl;
                                        System.Diagnostics.Debug.WriteLine($"[REALTIME] Using absolute URL: {processedAttachment.FileUrl}");
                                    }
                                    else if (attachment.FileUrl.StartsWith("/Uploads/"))
                                    {
                                        processedAttachment.FileUrl = $"http://localhost:5299{attachment.FileUrl}";
                                        System.Diagnostics.Debug.WriteLine($"[REALTIME] Fixed relative URL: {processedAttachment.FileUrl}");
                                    }
                                    else
                                    {
                                        string fileName = Path.GetFileName(attachment.FileUrl);
                                        processedAttachment.FileUrl = $"http://localhost:5299/Uploads/{fileName}";
                                        System.Diagnostics.Debug.WriteLine($"[REALTIME] Constructed URL: {processedAttachment.FileUrl}");
                                    }
                                }
                                else if (!string.IsNullOrEmpty(attachment.FileName))
                                {
                                    processedAttachment.FileUrl = $"http://localhost:5299/Uploads/{attachment.FileName}";
                                    System.Diagnostics.Debug.WriteLine($"[REALTIME] Created URL from filename: {processedAttachment.FileUrl}");
                                }

                                processedAttachments.Add(processedAttachment);
                            }

                            // Gán lại attachments đã xử lý
                            message.Attachments = processedAttachments;
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // CHỈ HIỂN THỊ NẾU ĐANG Ở TRONG CONVERSATION ĐÓ
                            if (SelectedConversation != null && message.ConversationId == SelectedConversation.Id)
                            {
                                // Kiểm tra tin nhắn đã tồn tại chưa
                                if (!Messages.Any(m => m.Id == message.Id))
                                {
                                    Messages.Add(message);

                                    // Cập nhật cache
                                    if (_conversationMessagesCache.ContainsKey(message.ConversationId))
                                    {
                                        _conversationMessagesCache[message.ConversationId].Add(message);
                                    }

                                    ScrollToLastMessage();
                                    System.Diagnostics.Debug.WriteLine($"[REALTIME] Added message to UI with {message.Attachments?.Count ?? 0} attachments");
                                }
                            }
                            else
                            {
                                // Nếu không phải conversation hiện tại, chỉ cập nhật cache
                                if (_conversationMessagesCache.ContainsKey(message.ConversationId))
                                {
                                    _conversationMessagesCache[message.ConversationId].Add(message);
                                }
                                else
                                {
                                    _conversationMessagesCache[message.ConversationId] = new List<Message> { message };
                                }
                                System.Diagnostics.Debug.WriteLine($"[REALTIME] Added message to cache for conversation {message.ConversationId}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[REALTIME] Error handling received message: {ex.Message}");
                    }
                });

                // Xử lý tin nhắn được chỉnh sửa
                _hubConnection.On<string, string>("MessageUpdated", (messageId, newContent) =>
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var message = Messages.FirstOrDefault(m => m.Id == messageId);
                            if (message != null)
                            {
                                message.Content = newContent;
                                message.IsEdited = true;
                                System.Diagnostics.Debug.WriteLine($"[REALTIME] Updated message: {messageId}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[REALTIME] Error updating message: {ex.Message}");
                    }
                });

                // Xử lý tin nhắn bị xóa
                _hubConnection.On<string>("MessageDeleted", (messageId) =>
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var message = Messages.FirstOrDefault(m => m.Id == messageId);
                            if (message != null)
                            {
                                Messages.Remove(message);
                                System.Diagnostics.Debug.WriteLine($"[REALTIME] Deleted message: {messageId}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[REALTIME] Error deleting message: {ex.Message}");
                    }
                });

                // Connection events
                _hubConnection.Reconnecting += (error) =>
                {
                    System.Diagnostics.Debug.WriteLine("[REALTIME] Reconnecting...");
                    Application.Current.Dispatcher.Invoke(() => IsConnected = false);
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += (connectionId) =>
                {
                    System.Diagnostics.Debug.WriteLine("[REALTIME] Reconnected!");
                    Application.Current.Dispatcher.Invoke(() => IsConnected = true);

                    // Rejoin current conversation
                    if (SelectedConversation != null)
                    {
                        _ = Task.Run(async () => await JoinConversationGroup(SelectedConversation.Id));
                    }
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += (error) =>
                {
                    System.Diagnostics.Debug.WriteLine("[REALTIME] Connection closed");
                    Application.Current.Dispatcher.Invoke(() => IsConnected = false);
                    return Task.CompletedTask;
                };

                // Khởi động kết nối
                await _hubConnection.StartAsync();
                IsConnected = true;
                System.Diagnostics.Debug.WriteLine("[REALTIME] Connected to SignalR Hub");

                // Join current conversation if any
                if (SelectedConversation != null)
                {
                    await JoinConversationGroup(SelectedConversation.Id);
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                System.Diagnostics.Debug.WriteLine($"[REALTIME] Error connecting to SignalR: {ex.Message}");
            }
        }

        // Thêm method helper
        private async Task JoinConversationGroup(string conversationId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("JoinGroup", conversationId);
                    System.Diagnostics.Debug.WriteLine($"[REALTIME] Joined group: {conversationId}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[REALTIME] Error joining group: {ex.Message}");
                }
            }
        }

        private async Task LeaveConversationGroup(string conversationId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("LeaveGroup", conversationId);
                    System.Diagnostics.Debug.WriteLine($"[REALTIME] Left group: {conversationId}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[REALTIME] Error leaving group: {ex.Message}");
                }
            }
        }

        // Thêm vào UserChatViewModel

        private void LoadDemoData()
        {
            // Demo user
            CurrentUser = new User
            {
                Id = "1",
                Username = "currentuser",
                DisplayName = "Current User"
            };

            // Demo conversations
            var conversations = new[]
            {
                new Conversation
                {
                    Id = "1",
                    Title = "Nguyen Dinh Khang",
                    LastActivity = DateTime.Now.AddMinutes(-10),
                    ParticipantIds = new List<string> { "1", "2" }
                },
                new Conversation
                {
                    Id = "2",
                    Title = "Another User",
                    LastActivity = DateTime.Now.AddMinutes(-30),
                    ParticipantIds = new List<string> { "1", "3" }
                }
            };

            Conversations = new ObservableCollection<Conversation>(conversations);

            // Demo users
            var users = new[]
            {
                new User { Id = "2", Username = "user2", DisplayName = "Nguyen Dinh Khang" },
                new User { Id = "3", Username = "user3", DisplayName = "Another User" }
            };

            Users = new ObservableCollection<User>(users);
        }
        // Thêm vào UserChatViewModel



        // Thêm phương thức này vào class UserChatViewModel
        private async Task VerifyCacheIntegrity(string conversationId)
        {
            try
            {
                if (!_conversationMessagesCache.ContainsKey(conversationId))
                {
                    System.Diagnostics.Debug.WriteLine($"Cache không tồn tại cho conversation: {conversationId}");
                    return;
                }

                var cachedCount = _conversationMessagesCache[conversationId].Count;

                // Kiểm tra với server
                var response = await _httpClient.GetAsync($"messages/conversation/{conversationId}/count");
                if (response.IsSuccessStatusCode)
                {
                    var serverCount = await response.Content.ReadFromJsonAsync<int>();
                    System.Diagnostics.Debug.WriteLine($"Kiểm tra cache: Local={cachedCount}, Server={serverCount}");

                    if (serverCount > cachedCount)
                    {
                        System.Diagnostics.Debug.WriteLine("Phát hiện thiếu tin nhắn trong cache, tải lại từ server...");
                        // Xóa cache hiện tại và tải lại
                        _conversationMessagesCache.Remove(conversationId);
                        await RefreshMessages(conversationId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi kiểm tra cache: {ex.Message}");
            }
        }
        private async Task LoadRealData()
        {
            try
            {
                // Get the current user from Properties which was set during login
                if (App.Current.Properties.Contains("CurrentUser"))
                {
                    CurrentUser = (User)App.Current.Properties["CurrentUser"];

                    // Get token (in a real application, you'd store and retrieve this properly)
                    string token = CurrentUser.Id; // Using ID as token for simplicity

                    // Initialize UserService
                    var userService = new UserService();

                    // Update current user with fresh data from server
                    try
                    {
                        var updatedUser = await userService.GetCurrentUserAsync(token);
                        if (updatedUser != null)
                        {
                            CurrentUser = updatedUser;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error refreshing user data: {ex.Message}");
                        // Continue with existing user data
                    }

                    // Load the user's conversations
                    await LoadConversations();
                }
                else
                {
                    // Fallback to demo data if no user is logged in
                    LoadDemoData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading real data: {ex.Message}");
                // Fallback to demo data
                LoadDemoData();
            }
        }
        private async Task InitializeAsync()
        {
            try
            {
                // Lấy thông tin user hiện tại (sau khi xác thực)
                var response = await _httpClient.GetAsync("users/current");
                if (response.IsSuccessStatusCode)
                {
                    CurrentUser = await response.Content.ReadFromJsonAsync<User>();

                    // Tải danh sách cuộc hội thoại
                    await LoadConversations();
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                MessageBox.Show($"Lỗi khởi tạo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NewConversation(object parameter)
        {
            try
            {
                var searchDialog = new FriendSearchDialog(CurrentUser); // Truyền CurrentUser vào
                if (searchDialog.ShowDialog() == true && searchDialog.SelectedUser != null)
                {
                    var selectedUser = searchDialog.SelectedUser;

                    // Tìm cuộc trò chuyện hiện có
                    var existingConversation = Conversations.FirstOrDefault(c =>
                        c.ParticipantIds.Contains(selectedUser.Id) &&
                        c.ParticipantIds.Count == 2);

                    if (existingConversation != null)
                    {
                        // Nếu đã tồn tại, chọn nó
                        SelectedConversation = existingConversation;
                    }
                    else
                    {
                        // Tạo cuộc trò chuyện mới
                        CreatePrivateConversation(selectedUser);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo cuộc trò chuyện mới: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Thêm phương thức để tạo cuộc trò chuyện riêng tư
        private async void CreatePrivateConversation(User user)
        {
            try
            {
                // Tạo cuộc trò chuyện mới
                var conversation = new Conversation
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Title = "", // Không cần title cho chat riêng tư
                    CreatorId = CurrentUser.Id,
                    ParticipantIds = new List<string> { CurrentUser.Id, user.Id },
                    ParticipantNames = new Dictionary<string, string>
                    {
                        { CurrentUser.Id, CurrentUser.DisplayName },
                        { user.Id, user.DisplayName }
                    },
                    CreatedAt = DateTime.UtcNow,
                    LastMessageTimestamp = DateTime.UtcNow
                };

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
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo cuộc trò chuyện: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Thêm phương thức ShowAttachmentsPanel còn thiếu
        private void ShowAttachmentsPanel(object parameter)
        {
            IsAttachmentsPanelOpen = !IsAttachmentsPanelOpen;
        }

        // Thêm phương thức NavigateToLogin còn thiếu
        private void NavigateToLogin()
        {
            // Chuyển về trang đăng nhập
            Application.Current.Dispatcher.Invoke(() =>
            {
                var loginView = new LoginView();
                loginView.Show();

                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.Close();
                }
            });
        }
        private async void SelectFile(object parameter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "All files (*.*)|*.*"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    foreach (string filename in dialog.FileNames)
                    {
                        var fileInfo = new FileInfo(filename);
                        if (fileInfo.Length > 10485760) // 10MB
                        {
                            MessageBox.Show($"File {fileInfo.Name} quá lớn. Kích thước tối đa là 10MB.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }

                        // Tạo attachment tạm thời để hiển thị
                        var tempAttachment = new Attachment
                        {
                            Id = Guid.NewGuid().ToString(),
                            FileName = fileInfo.Name,
                            ContentType = GetMimeType(fileInfo.Extension),
                            FileSize = fileInfo.Length,
                            FileUrl = filename, // Đường dẫn local tạm thời
                            ThumbnailUrl = null
                        };

                        // Thêm vào danh sách đính kèm
                        SelectedAttachments.Add(tempAttachment);
                    }

                    // Hiển thị panel đính kèm
                    IsAttachmentsPanelOpen = true;

                    // Kích hoạt nút gửi tin nhắn
                    (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi chọn file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        // Phương thức chọn hình ảnh
        private void SelectImage(object parameter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif|All files (*.*)|*.*"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    foreach (string filename in dialog.FileNames)
                    {
                        var fileInfo = new FileInfo(filename);
                        if (fileInfo.Length > 10485760) // 10MB
                        {
                            MessageBox.Show($"Hình ảnh {fileInfo.Name} quá lớn. Kích thước tối đa là 10MB.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }

                        // Tạo attachment tạm thời để hiển thị
                        var tempAttachment = new Attachment
                        {
                            Id = Guid.NewGuid().ToString(),
                            FileName = fileInfo.Name,
                            ContentType = "image/" + fileInfo.Extension.TrimStart('.').ToLower(),
                            FileSize = fileInfo.Length,
                            FileUrl = filename, // Đường dẫn local tạm thời
                            ThumbnailUrl = null,
                        };

                        // Thêm vào danh sách đính kèm
                        SelectedAttachments.Add(tempAttachment);
                    }

                    // Hiển thị panel đính kèm
                    IsAttachmentsPanelOpen = true;

                    // Kích hoạt nút gửi tin nhắn
                    (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi chọn hình ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Phương thức xóa file đính kèm
        private void RemoveAttachment(object parameter)
        {
            if (parameter is Attachment attachment)
            {
                SelectedAttachments.Remove(attachment);

                // Ẩn panel nếu không còn file đính kèm
                if (SelectedAttachments.Count == 0)
                    IsAttachmentsPanelOpen = false;

                // Cập nhật trạng thái nút gửi
                (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // Phương thức gửi tin nhắn có đính kèm




        // Helper method để lấy MIME type từ extension
        private string GetMimeType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".zip": return "application/zip";
                case ".rar": return "application/x-rar-compressed";
                default: return "application/octet-stream";
            }
        }
        private async Task LoadConversations()
        {
            try
            {
                var response = await _httpClient.GetAsync($"conversations/user/{CurrentUser.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var conversations = await response.Content.ReadFromJsonAsync<List<Conversation>>();
                    if (conversations != null)
                    {
                        // For each conversation, fetch user details
                        foreach (var conversation in conversations)
                        {
                            conversation.Participants = await GetParticipantsForConversation(conversation);
                        }

                        Conversations = new ObservableCollection<Conversation>(conversations);

                        // Select the first conversation if any
                        if (Conversations.Count > 0 && SelectedConversation == null)
                        {
                            SelectedConversation = Conversations[0];
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load conversations: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading conversations: {ex.Message}");
                MessageBox.Show($"Lỗi tải cuộc trò chuyện: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<List<User>> GetParticipantsForConversation(Conversation conversation)
        {
            var participants = new List<User>();

            foreach (var userId in conversation.ParticipantIds)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"user/{userId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var user = await response.Content.ReadFromJsonAsync<User>();
                        if (user != null)
                        {
                            participants.Add(user);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error fetching user {userId}: {ex.Message}");
                }
            }

            return participants;
        }

        private bool CanSendMessage(object parameter)
        {
            return ((!string.IsNullOrWhiteSpace(CurrentMessage) || SelectedAttachments.Count > 0) &&
                    SelectedConversation != null);
        }
        private async void SendMessage(object parameter)
        {
            if ((string.IsNullOrWhiteSpace(CurrentMessage) && SelectedAttachments.Count == 0) ||
        SelectedConversation == null || CurrentUser == null)
                return;

            try
            {
                var messageContent = CurrentMessage.Trim();
                var conversationId = SelectedConversation.Id;
                var messageId = ObjectId.GenerateNewId().ToString(); // Tạo ID trước


                // Tạo message object
                var newMessage = new Message
                {
                    Id = messageId,
                    ConversationId = conversationId,
                    SenderId = CurrentUser.Id,
                    SenderName = CurrentUser.DisplayName ?? CurrentUser.Username,
                    Content = messageContent,
                    Timestamp = DateTimeHelper.GetVietnamTime(),
                    Type = MessageType.Text,
                    IsRead = false,
                    IsEdited = false,
                    Attachments = new List<Attachment>()
                };

                // UPLOAD FILES TRƯỚC KHI GỬI MESSAGE
                if (SelectedAttachments.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SEND] Uploading {SelectedAttachments.Count} files");

                    foreach (var attachment in SelectedAttachments)
                    {
                        try
                        {
                            attachment.MessageId = messageId;

                            // Upload file lên server
                            var uploadedAttachment = await UploadFileAsync(attachment);
                            if (uploadedAttachment != null)
                            {
                                // Gán MessageId cho attachment

                                uploadedAttachment.MessageId = newMessage.Id;
                                newMessage.Attachments.Add(uploadedAttachment);
                                System.Diagnostics.Debug.WriteLine($"[SEND] Uploaded: {uploadedAttachment.FileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SEND] Error uploading {attachment.FileName}: {ex.Message}");
                            MessageBox.Show($"Lỗi upload file {attachment.FileName}: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    // Cập nhật message type
                    if (newMessage.Attachments.Any(a => a.IsImage))
                    {
                        newMessage.Type = MessageType.Image;
                    }
                    else if (newMessage.Attachments.Any())
                    {
                        newMessage.Type = MessageType.File;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[SEND] Sending message with {newMessage.Attachments.Count} attachments");

                // Gửi message đến server
                var saveResponse = await _httpClient.PostAsJsonAsync("messages", newMessage);
                if (!saveResponse.IsSuccessStatusCode)
                {
                    var errorContent = await saveResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Không thể lưu tin nhắn: {errorContent}");
                }

                System.Diagnostics.Debug.WriteLine("[SEND] Message saved to database");

                // 2. HIỂN THỊ NGAY LẬP TỨC TRÊN UI CỦA NGƯỜI GỬI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(newMessage);

                    // Cập nhật cache
                    if (_conversationMessagesCache.ContainsKey(conversationId))
                    {
                        _conversationMessagesCache[conversationId].Add(newMessage);
                    }
                    else
                    {
                        _conversationMessagesCache[conversationId] = new List<Message> { newMessage };
                    }

                    // Clear input
                    CurrentMessage = string.Empty;
                    SelectedAttachments.Clear();
                    IsAttachmentsPanelOpen = false;

                    ScrollToLastMessage();
                });

                System.Diagnostics.Debug.WriteLine("[SEND] Message added to sender's UI");

                // 3. GỬI QUA SIGNALR ĐẾN CÁC CLIENT KHÁC
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await _hubConnection.InvokeAsync("SendMessage",
                            conversationId,
                            CurrentUser.Id,
                            CurrentUser.DisplayName ?? CurrentUser.Username,
                            messageContent);

                        System.Diagnostics.Debug.WriteLine("[SEND] Message broadcasted via SignalR");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SEND] SignalR error: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SEND] SignalR not connected, message not broadcasted");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SEND] Error: {ex.Message}");
                MessageBox.Show($"Lỗi gửi tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SearchFriends(object parameter)
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("Vui lòng đăng nhập để sử dụng tính năng này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var searchDialog = new FriendSearchDialog(CurrentUser);
                var result = searchDialog.ShowDialog();

                if (result == true && searchDialog.DialogConfirmed && searchDialog.SelectedUser != null)
                {
                    // Create a new conversation with the selected user
                    var userService = new UserService();
                    var newConversation = await userService.CreatePrivateConversationAsync(searchDialog.SelectedUser.Id);

                    if (newConversation != null)
                    {
                        // Add to conversations collection if not already there
                        if (!Conversations.Any(c => c.Id == newConversation.Id))
                        {
                            Conversations.Add(newConversation);
                        }

                        // Select the new conversation
                        SelectedConversation = Conversations.First(c => c.Id == newConversation.Id);

                        // Sort conversations by last activity
                        SortConversations();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo cuộc trò chuyện: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task<Attachment> UploadFileAsync(Attachment tempAttachment)
        {
            try
            {
                // Đọc file từ đường dẫn local
                if (!File.Exists(tempAttachment.FileUrl))
                {
                    throw new FileNotFoundException($"File không tồn tại: {tempAttachment.FileUrl}");
                }

                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(tempAttachment.FileUrl));
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(tempAttachment.ContentType);
                    content.Add(fileContent, "file", tempAttachment.FileName);
                    content.Add(new StringContent(tempAttachment.MessageId ?? ""), "MessageId");
                    content.Add(new StringContent(CurrentUser.Id), "UploaderId");
                    content.Add(new StringContent(tempAttachment.FileName), "FileName");
                    content.Add(new StringContent(tempAttachment.ContentType), "ContentType");

                    System.Diagnostics.Debug.WriteLine($"[UPLOAD] Uploading file: {tempAttachment.FileName}");
                    System.Diagnostics.Debug.WriteLine($"[UPLOAD] MessageId: {tempAttachment.MessageId}");
                    System.Diagnostics.Debug.WriteLine($"[UPLOAD] UploaderId: {CurrentUser.Id}");

                    // Gửi file lên server
                    var response = await _httpClient.PostAsync("attachments/upload", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var uploadedAttachment = await response.Content.ReadFromJsonAsync<Attachment>();
                        return uploadedAttachment;
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Upload failed: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload error: {ex.Message}");
                throw;
            }
        }
        private async void CreateGroup(object parameter)
        {
            try
            {
                // Show the CreateGroupDialog
                var createGroupDialog = new CreateGroupDialog();
                var result = createGroupDialog.ShowDialog();

                if (result == true)
                {
                    string groupName = createGroupDialog.GroupName;
                    List<User> selectedUsers = createGroupDialog.SelectedUsers;

                    // Create the participant list starting with current user
                    List<string> participantIds = new List<string> { CurrentUser.Id };

                    // Add selected users
                    participantIds.AddRange(selectedUsers.Select(u => u.Id));

                    // Create the group members list with current user as owner
                    List<GroupMember> groupMembers = new List<GroupMember>
            {
                new GroupMember
                {
                    UserId = CurrentUser.Id,
                    Role = GroupRole.Owner,
                    JoinedAt = DateTime.Now
                }
            };

                    // Add selected users as members
                    foreach (var user in selectedUsers)
                    {
                        groupMembers.Add(new GroupMember
                        {
                            UserId = user.Id,
                            Role = GroupRole.Member,
                            JoinedAt = DateTime.Now
                        });
                    }

                    // Generate a new ID here instead of letting the server do it
                    string newGroupId = ObjectId.GenerateNewId().ToString();

                    // Create new group conversation with the required fields
                    var newGroup = new Conversation
                    {
                        Id = newGroupId,                  // Set Id explicitly
                        Title = groupName,
                        IsGroup = true,
                        CreatorId = CurrentUser.Id,
                        ParticipantIds = participantIds,
                        LastActivity = DateTime.Now,
                        LastMessageId = "",               // Provide empty but not null
                        GroupMembers = groupMembers
                    };

                    // Send request to server
                    var response = await _httpClient.PostAsJsonAsync("conversations/group", newGroup);

                    if (response.IsSuccessStatusCode)
                    {
                        var createdGroup = await response.Content.ReadFromJsonAsync<Conversation>();

                        // Add to local collection and select it
                        if (createdGroup != null)
                        {
                            // Get participants to add to the conversation object
                            createdGroup.Participants = await GetParticipantsForConversation(createdGroup);

                            Conversations.Add(createdGroup);
                            SelectedConversation = createdGroup;

                            // Sort conversations by last activity
                            SortConversations();

                            // Show success message
                            MessageBox.Show($"Nhóm '{groupName}' đã được tạo thành công với {selectedUsers.Count} thành viên.",
                                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Máy chủ trả về lỗi: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tạo nhóm: {ex.Message}",
                              "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchConversations()
        {
            // Lọc cuộc hội thoại dựa trên từ khóa tìm kiếm
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Reset về danh sách đầy đủ
                LoadConversations();
            }
            else
            {
                var filteredConversations = Conversations
                    .Where(c => c.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Conversations = new ObservableCollection<Conversation>(filteredConversations);
            }
        }

        private void SortConversations()
        {
            var sortedConversations = Conversations
                .OrderByDescending(c => c.LastActivity)
                .ToList();

            Conversations = new ObservableCollection<Conversation>(sortedConversations);
        }

        private void CreateNewConversation(object parameter)
        {
            // Hiển thị dialog để chọn người dùng
            // Sau đó tạo cuộc hội thoại mới
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Func<T, bool> _canExecute;

            public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                if (parameter is T typedParameter)
                {
                    return _canExecute == null || _canExecute(typedParameter);
                }
                return _canExecute == null;
            }

            public void Execute(object parameter)
            {
                if (parameter is T typedParameter)
                {
                    _execute(typedParameter);
                }
                else if (parameter == null && !typeof(T).IsValueType)
                {
                    _execute(default(T));
                }
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void RaiseCanExecuteChanged()
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // RelayCommand implementation
        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null || _canExecute(parameter);
            }

            public void Execute(object parameter)
            {
                _execute(parameter);
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void RaiseCanExecuteChanged()
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Navigation Methods
        private void NavigateToHome(object parameter)
        {
            var homeView = new HomePageView();
            homeView.Show();
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is UserChatView)?.Close();
        }

        private void NavigateToChat(object parameter)
        {
            // Already in chat view, do nothing
        }

        private void NavigateToActivities(object parameter)
        {
            var activitiesView = new ActivitiesView();
            activitiesView.Show();
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is UserChatView)?.Close();
        }

        private void NavigateToMembers(object parameter)
        {
            var membersView = new MembersView();
            membersView.Show();
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is UserChatView)?.Close();
        }

        private void NavigateToTasks(object parameter)
        {
            var tasksView = new TasksView();
            tasksView.Show();
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is UserChatView)?.Close();
        }
    }
}