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
namespace DoanKhoaClient.ViewModels
{
    public class UserChatViewModel : INotifyPropertyChanged
    {
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
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<User> _groupMembers = new ObservableCollection<User>();
        private bool _isGroupDetailVisible = false;

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


        public ObservableCollection<User> GroupMembers
        {
            get => _groupMembers;
            set
            {
                _groupMembers = value;
                OnPropertyChanged();
            }
        }

        public bool IsGroupDetailVisible
        {
            get => _isGroupDetailVisible;
            set
            {
                _isGroupDetailVisible = value;
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
        public ICommand DeleteMessageCommand { get; private set; }
        public ICommand ShowGroupDetailCommand { get; private set; } // Add this line
        public ICommand RemoveUserFromGroupCommand { get; private set; } // Add this line

        private ICommand _searchFriendsCommand;
        public ICommand SearchFriendsCommand => _searchFriendsCommand ??= new RelayCommand(SearchFriends);
        public UserChatViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };

            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
            NewConversationCommand = new RelayCommand(CreateNewConversation);
            SelectFileCommand = new RelayCommand(SelectFile);
            SelectImageCommand = new RelayCommand(SelectImage);
            RemoveAttachmentCommand = new RelayCommand(RemoveAttachment);
            CreateGroupCommand = new RelayCommand(CreateGroup);
            ShowAttachmentsPanelCommand = new RelayCommand(_ => IsAttachmentsPanelOpen = !IsAttachmentsPanelOpen);
            DeleteMessageCommand = new RelayCommand(DeleteMessage);  // Thêm dòng này
            ShowGroupDetailCommand = new RelayCommand(_ => ToggleGroupDetail());
            RemoveUserFromGroupCommand = new RelayCommand(RemoveUserFromGroup);

            LoadRealData();

            // Kết nối SignalR
            ConnectToHub();
        }

        private async Task ConnectToHub()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5299/chatHub")
                    .WithAutomaticReconnect()
                    .Build();
                _hubConnection.On<string>("MessageDeleted", (messageId) =>
                {
                    try
                    {
                        Console.WriteLine($"Received MessageDeleted for ID: {messageId}");

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var messageToRemove = Messages.FirstOrDefault(m => m.Id == messageId);
                            if (messageToRemove != null)
                            {
                                Console.WriteLine("Removing message from UI");
                                Messages.Remove(messageToRemove);
                            }
                            else
                            {
                                Console.WriteLine("Message not found in Messages collection");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in MessageDeleted handler: {ex.Message}");
                    }
                });

                // Thêm sự kiện xử lý khi user bị xóa khỏi nhóm
                _hubConnection.On<string, Message>("UserRemovedFromGroup", (userId, systemMessage) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Nếu người dùng hiện tại bị xóa
                        if (userId == CurrentUser.Id)
                        {
                            MessageBox.Show("Bạn đã bị xóa khỏi nhóm này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Xóa conversation hiện tại nếu cần
                            var conversationToRemove = Conversations.FirstOrDefault(c => c.Id == systemMessage.ConversationId);
                            if (conversationToRemove != null)
                            {
                                Conversations.Remove(conversationToRemove);
                                SelectedConversation = null;
                            }
                        }
                        else
                        {
                            // Hiển thị thông báo hệ thống trong chat
                            if (systemMessage.ConversationId == _selectedConversation?.Id)
                            {
                                Messages.Add(systemMessage);
                            }

                            // Cập nhật danh sách thành viên
                            if (IsGroupDetailVisible)
                            {
                                var memberToRemove = GroupMembers.FirstOrDefault(m => m.Id == userId);
                                if (memberToRemove != null)
                                {
                                    GroupMembers.Remove(memberToRemove);
                                }
                            }
                        }
                    });
                });

                _hubConnection.On<Message>("ReceiveMessage", (message) =>
                {
                    try
                    {
                        // Kiểm tra xem tin nhắn là thông báo tạo nhóm không
                        bool isGroupCreatedMessage = message.Type == MessageType.System &&
                                                    message.Content.Contains("has been created");

                        // Nếu là tin nhắn thông báo tạo nhóm
                        if (isGroupCreatedMessage)
                        {
                            // Kiểm tra xem tin nhắn có được hiển thị trong cuộc trò chuyện hiện tại không
                            if (message.ConversationId == _selectedConversation?.Id)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    // Kiểm tra nếu đã có thông báo tạo nhóm trong danh sách
                                    bool alreadyHasCreationMessage = Messages.Any(m =>
                                        m.Type == MessageType.System &&
                                        m.Content.Contains("has been created"));

                                    // Nếu chưa có thì mới thêm vào
                                    if (!alreadyHasCreationMessage)
                                    {
                                        Messages.Add(message);
                                    }
                                });
                            }
                        }
                        // Nếu là tin nhắn thông thường
                        else if (message.ConversationId == _selectedConversation?.Id)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Kiểm tra tin nhắn đã tồn tại chưa và không phải do chính mình gửi
                                if (!Messages.Any(m => m.Id == message.Id) && message.SenderId != CurrentUser.Id)
                                {
                                    Messages.Add(message);
                                }
                            });
                        }

                        // Cập nhật LastActivity cho cuộc hội thoại tương ứng
                        var conversation = Conversations.FirstOrDefault(c => c.Id == message.ConversationId);
                        if (conversation != null)
                        {
                            conversation.LastActivity = message.Timestamp;
                            // Sắp xếp lại conversations theo thời gian
                            SortConversations();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling received message: {ex.Message}");
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
        private async void DeleteMessage(object parameter)
        {
            if (parameter is Message message)
            {
                MessageBox.Show($"Bạn có chắc muốn xóa tin nhắn này không?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                // Kiểm tra quyền xóa (người gửi hoặc admin)
                if (message.SenderId != CurrentUser.Id && CurrentUser.Role != UserRole.Admin)
                {
                    MessageBox.Show("Bạn không có quyền xóa tin nhắn này.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show("Bạn có chắc muốn xóa tin nhắn này?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                        {
                            Console.WriteLine($"Deleting message with ID: {message.Id}");
                            await _hubConnection.InvokeAsync("DeleteMessage", message.Id);

                            // Xóa tin nhắn khỏi UI ngay lập tức để phản hồi nhanh
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Messages.Remove(message);
                            });
                        }
                        else
                        {
                            MessageBox.Show("Không thể kết nối với máy chủ", "Lỗi kết nối",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa tin nhắn: {ex.Message}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
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

        // Thêm phương thức mới
        private void ToggleGroupDetail()
        {
            if (SelectedConversation != null && SelectedConversation.IsGroup)
            {
                IsGroupDetailVisible = !IsGroupDetailVisible;

                if (IsGroupDetailVisible)
                {
                    LoadGroupMembers();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một cuộc trò chuyện nhóm.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void LoadGroupMembers()
        {
            if (SelectedConversation == null || !SelectedConversation.IsGroup) return;

            try
            {
                // Xóa danh sách members hiện tại
                GroupMembers.Clear();

                // Lấy thông tin tất cả người dùng
                var response = await _httpClient.GetAsync($"conversations/{SelectedConversation.Id}/members");
                if (response.IsSuccessStatusCode)
                {
                    var members = await response.Content.ReadFromJsonAsync<List<User>>();
                    if (members != null)
                    {
                        foreach (var member in members)
                        {
                            GroupMembers.Add(member);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thành viên: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveUserFromGroup(object parameter)
        {
            if (parameter is User user && SelectedConversation != null && SelectedConversation.IsGroup)
            {
                // Kiểm tra quyền xóa thành viên
                if (CurrentUser.Id != SelectedConversation.CreatorId && CurrentUser.Role != UserRole.Admin)
                {
                    MessageBox.Show("Bạn không có quyền xóa thành viên khỏi nhóm.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (user.Id == SelectedConversation.CreatorId)
                {
                    MessageBox.Show("Không thể xóa người tạo nhóm.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"Bạn có chắc muốn xóa {user.DisplayName} khỏi nhóm?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync("RemoveUserFromGroup",
                                SelectedConversation.Id, user.Id);

                            // Cập nhật UI
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                GroupMembers.Remove(user);

                                // Hiển thị thông báo
                                MessageBox.Show($"Đã xóa {user.DisplayName} khỏi nhóm.",
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa người dùng khỏi nhóm: {ex.Message}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
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
        private async Task LoadMessages(string conversationId)
        {
            try
            {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await _hubConnection.InvokeAsync("JoinGroup", conversationId);
                        Console.WriteLine($"Joined group {conversationId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error joining group: {ex.Message}");
                    }
                }

                Messages.Clear();

                // Add debugging to see what's happening
                System.Diagnostics.Debug.WriteLine($"Loading messages for conversation: {conversationId}");

                // Use try-catch specifically for the API call to better handle errors
                try
                {
                    var response = await _httpClient.GetAsync($"messages/conversation/{conversationId}");
                    System.Diagnostics.Debug.WriteLine($"API response status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
                        System.Diagnostics.Debug.WriteLine($"Retrieved {messages.Count} messages");

                        if (messages != null && messages.Count > 0)
                        {
                            Messages = new ObservableCollection<Message>(messages);
                            return; // Successfully loaded messages, exit method
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Retrieved empty message list");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"API error: {errorContent}");
                    }
                }
                catch (Exception apiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"API exception: {apiEx.Message}");
                    // Don't show message box here, just log the error and continue with fallback
                }

                // Only reach here if API call failed or returned no messages
                // In production, you might want to show empty message list instead of demo data
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                MessageBox.Show($"Lỗi tải tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SendMessage(object parameter)
        {
            // Kiểm tra điều kiện gửi
            if ((string.IsNullOrWhiteSpace(CurrentMessage) && SelectedAttachments.Count == 0) ||
                SelectedConversation == null)
                return;

            try
            {
                var newMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(), // ID tạm thời
                    ConversationId = SelectedConversation.Id,
                    SenderId = CurrentUser.Id,
                    Content = CurrentMessage,
                    Timestamp = DateTimeHelper.GetVietnamTime(), // Thay cho DateTime.UtcNow
                    Attachments = new List<Attachment>()
                };

                // Xác định loại tin nhắn
                if (SelectedAttachments.Count > 0)
                {
                    bool allImages = SelectedAttachments.All(a => a.IsImage);
                    if (allImages && string.IsNullOrWhiteSpace(CurrentMessage))
                        newMessage.Type = MessageType.Image;
                    else if (!allImages && string.IsNullOrWhiteSpace(CurrentMessage))
                        newMessage.Type = MessageType.File;
                    else
                        newMessage.Type = MessageType.Text;
                }

                // Tải các file đính kèm lên server
                var serverAttachments = new List<Attachment>();
                foreach (var attachment in SelectedAttachments)
                {
                    try
                    {
                        // Đọc nội dung file
                        var fileBytes = System.IO.File.ReadAllBytes(attachment.FileUrl);

                        // Tạo form data
                        using var content = new MultipartFormDataContent();
                        using var fileContent = new ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(attachment.ContentType);
                        content.Add(fileContent, "File", attachment.FileName);
                        content.Add(new StringContent(CurrentUser.Id), "UploaderId");

                        // Gửi file lên server
                        var response = await _httpClient.PostAsync("attachments/upload", content);

                        if (response.IsSuccessStatusCode)
                        {
                            var uploadedAttachment = await response.Content.ReadFromJsonAsync<Attachment>();
                            if (uploadedAttachment != null)
                            {
                                serverAttachments.Add(uploadedAttachment);
                            }
                        }
                        else
                        {
                            throw new Exception($"Không thể tải file {attachment.FileName}: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Lỗi khi tải file {attachment.FileName}: {ex.Message}");
                    }
                }

                // Cập nhật tin nhắn với các file đã tải lên
                newMessage.Attachments = serverAttachments;

                // Thêm tin nhắn vào danh sách hiển thị ngay lập tức
                Messages.Add(newMessage);

                // Xóa tin nhắn hiện tại và danh sách file đã chọn
                CurrentMessage = string.Empty;
                SelectedAttachments.Clear();
                IsAttachmentsPanelOpen = false;

                // Gửi tin nhắn qua SignalR
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    if (serverAttachments.Count > 0)
                    {
                        await _hubConnection.InvokeAsync("SendMessageWithAttachments",
                            SelectedConversation.Id,
                            CurrentUser.Id,
                            newMessage.Content,
                            serverAttachments,
                            newMessage.Type);
                    }
                    else
                    {
                        await _hubConnection.InvokeAsync("SendMessage",
                            SelectedConversation.Id,
                            CurrentUser.Id,
                            newMessage.Content);
                    }
                }
            }
            catch (Exception ex)
            {
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
    }
}