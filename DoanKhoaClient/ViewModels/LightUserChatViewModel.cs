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
namespace DoanKhoaClient.ViewModels
{
    public class LightUserChatViewModel : INotifyPropertyChanged
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
        // Commands
        public ICommand SendMessageCommand { get; private set; }
        public ICommand NewConversationCommand { get; private set; }
        public ICommand SelectFileCommand { get; private set; }
        public ICommand SelectImageCommand { get; private set; }
        public ICommand RemoveAttachmentCommand { get; private set; }
        public ICommand CreateGroupCommand { get; private set; }
        public ICommand ShowAttachmentsPanelCommand { get; private set; }

        public LightUserChatViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };

            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
            NewConversationCommand = new RelayCommand(CreateNewConversation);
            SelectFileCommand = new RelayCommand(SelectFile);
            SelectImageCommand = new RelayCommand(SelectImage);
            RemoveAttachmentCommand = new RelayCommand(RemoveAttachment);
            CreateGroupCommand = new RelayCommand(CreateGroup);
            ShowAttachmentsPanelCommand = new RelayCommand(_ => IsAttachmentsPanelOpen = !IsAttachmentsPanelOpen);

            // Demo data cho đến khi kết nối với backend
            LoadDemoData();

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

                _hubConnection.On<Message>("ReceiveMessage", (message) =>
                {
                    try
                    {
                        // Thêm log để debug
                        Console.WriteLine($"Received message: {message.Id}, ConvId: {message.ConversationId}, Current ConvId: {_selectedConversation?.Id}");

                        // Chỉ thêm tin nhắn vào nếu nó thuộc cuộc hội thoại hiện tại
                        if (message.ConversationId == _selectedConversation?.Id)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Kiểm tra tin nhắn đã tồn tại chưa để tránh trùng lặp
                                if (!Messages.Any(m => m.Id == message.Id) &&
                                    message.SenderId != CurrentUser.Id) // Không thêm tin nhắn của chính mình
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


        // Phương thức tạo nhóm chat mới
        private async void CreateGroup(object parameter)
        {
            // Tạo dialog để nhập tên nhóm và chọn thành viên
            var dialog = new CreateGroupDialog();

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var newGroup = new Conversation
                    {
                        Title = dialog.GroupName,
                        IsGroup = true,
                        CreatorId = CurrentUser.Id,
                        ParticipantIds = new List<string>(dialog.SelectedUsers.Select(u => u.Id)),
                        LastActivity = DateTime.Now
                    };

                    // Thêm người tạo nhóm vào danh sách thành viên
                    if (!newGroup.ParticipantIds.Contains(CurrentUser.Id))
                        newGroup.ParticipantIds.Add(CurrentUser.Id);

                    // Thêm creator vào nhóm với vai trò Owner
                    newGroup.GroupMembers.Add(new GroupMember
                    {
                        UserId = CurrentUser.Id,
                        Role = GroupRole.Owner,
                        JoinedAt = DateTime.Now
                    });

                    // Thêm các thành viên khác
                    foreach (var userId in newGroup.ParticipantIds.Where(id => id != CurrentUser.Id))
                    {
                        newGroup.GroupMembers.Add(new GroupMember
                        {
                            UserId = userId,
                            Role = GroupRole.Member,
                            JoinedAt = DateTime.Now
                        });
                    }

                    if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                    {
                        await _hubConnection.InvokeAsync("CreateGroupConversation", newGroup);
                    }
                    else
                    {
                        // Fallback nếu không có kết nối SignalR
                        var response = await _httpClient.PostAsJsonAsync("conversations", newGroup);
                        if (response.IsSuccessStatusCode)
                        {
                            var createdGroup = await response.Content.ReadFromJsonAsync<Conversation>();
                            Conversations.Add(createdGroup);
                            SelectedConversation = createdGroup;
                        }
                        else
                        {
                            MessageBox.Show($"Không thể tạo nhóm chat: {response.StatusCode}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo nhóm chat: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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
                    Conversations = new ObservableCollection<Conversation>(conversations);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                MessageBox.Show($"Lỗi tải danh sách hội thoại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                // Gọi API để lấy lịch sử tin nhắn
                var response = await _httpClient.GetAsync($"messages/conversation/{conversationId}");
                if (response.IsSuccessStatusCode)
                {
                    var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
                    Messages = new ObservableCollection<Message>(messages);
                }
                else
                {
                    // Tạo dữ liệu mẫu cho demo
                    var demoMessages = new[]
                    {
                        new Message
                        {
                            Id = "1",
                            ConversationId = conversationId,
                            SenderId = "2", // Other user
                            Content = "Chào bạn!",
                            Timestamp = DateTime.Now.AddMinutes(-15)
                        },
                        new Message
                        {
                            Id = "2",
                            ConversationId = conversationId,
                            SenderId = CurrentUser.Id, // Current user
                            Content = "Xin chào! Bạn khỏe không?",
                            Timestamp = DateTime.Now.AddMinutes(-14)
                        },
                        new Message
                        {
                            Id = "3",
                            ConversationId = conversationId,
                            SenderId = "2", // Other user
                            Content = "Mình khỏe, cảm ơn bạn đã hỏi thăm!",
                            Timestamp = DateTime.Now.AddMinutes(-10)
                        }
                    };

                    Messages = new ObservableCollection<Message>(demoMessages);
                }
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
                    Timestamp = DateTime.Now,
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