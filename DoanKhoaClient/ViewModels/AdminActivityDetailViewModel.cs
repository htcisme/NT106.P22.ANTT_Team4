using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DoanKhoaClient.ViewModels
{
    public class AdminActivityDetailViewModel : INotifyPropertyChanged
    {
        private readonly ActivityService _activityService;
        private readonly CommentService _commentService;
        private readonly UserService _userService;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;
        private DispatcherTimer _autoRefreshTimer;

        private Activity _activity;
        private ObservableCollection<User> _participants;
        private ObservableCollection<User> _likedUsers;
        private ObservableCollection<Comment> _comments;
        private int _commentsCount;
        private int _rootCommentsCount;
        private int _repliesCount;
        private bool _isLoading;
        private bool _isLoadingComments;
        private bool _autoRefreshEnabled = true;
        private string _errorMessage;

        // Properties
        public Activity Activity
        {
            get => _activity;
            set
            {
                _activity = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<User> Participants
        {
            get => _participants;
            set
            {
                _participants = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<User> LikedUsers
        {
            get => _likedUsers;
            set
            {
                _likedUsers = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Comment> Comments
        {
            get => _comments;
            set
            {
                _comments = value;
                OnPropertyChanged();
                UpdateCommentsStatistics();
            }
        }

        public int CommentsCount
        {
            get => _commentsCount;
            set
            {
                _commentsCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNoComments));
            }
        }

        public int RootCommentsCount
        {
            get => _rootCommentsCount;
            set
            {
                _rootCommentsCount = value;
                OnPropertyChanged();
            }
        }

        public int RepliesCount
        {
            get => _repliesCount;
            set
            {
                _repliesCount = value;
                OnPropertyChanged();
            }
        }

        public bool HasNoComments => CommentsCount == 0 && !IsLoadingComments;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsLoadingComments
        {
            get => _isLoadingComments;
            set
            {
                _isLoadingComments = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNoComments));
            }
        }

        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                _autoRefreshEnabled = value;
                OnPropertyChanged();
                HandleAutoRefreshToggle();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        // Commands - Sử dụng backing fields
        private ICommand _editActivityCommand;
        private ICommand _deleteActivityCommand;
        private ICommand _loadParticipantsCommand;
        private ICommand _loadLikedUsersCommand;
        private ICommand _loadCommentsCommand;
        private ICommand _refreshParticipantsCommand;
        private ICommand _refreshLikesCommand;
        private ICommand _refreshCommentsCommand;
        private ICommand _removeParticipantCommand;
        private ICommand _removeLikeCommand;
        private ICommand _editCommentCommand;
        private ICommand _deleteCommentCommand;
        private ICommand _viewCommentThreadCommand;

        public ICommand EditActivityCommand
        {
            get => _editActivityCommand;
            private set
            {
                _editActivityCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand DeleteActivityCommand
        {
            get => _deleteActivityCommand;
            private set
            {
                _deleteActivityCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadParticipantsCommand
        {
            get => _loadParticipantsCommand;
            private set
            {
                _loadParticipantsCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadLikedUsersCommand
        {
            get => _loadLikedUsersCommand;
            private set
            {
                _loadLikedUsersCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadCommentsCommand
        {
            get => _loadCommentsCommand;
            private set
            {
                _loadCommentsCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshParticipantsCommand
        {
            get => _refreshParticipantsCommand;
            private set
            {
                _refreshParticipantsCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshLikesCommand
        {
            get => _refreshLikesCommand;
            private set
            {
                _refreshLikesCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommentsCommand
        {
            get => _refreshCommentsCommand;
            private set
            {
                _refreshCommentsCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand RemoveParticipantCommand
        {
            get => _removeParticipantCommand;
            private set
            {
                _removeParticipantCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand RemoveLikeCommand
        {
            get => _removeLikeCommand;
            private set
            {
                _removeLikeCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand EditCommentCommand
        {
            get => _editCommentCommand;
            private set
            {
                _editCommentCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand DeleteCommentCommand
        {
            get => _deleteCommentCommand;
            private set
            {
                _deleteCommentCommand = value;
                OnPropertyChanged();
            }
        }

        public ICommand ViewCommentThreadCommand
        {
            get => _viewCommentThreadCommand;
            private set
            {
                _viewCommentThreadCommand = value;
                OnPropertyChanged();
            }
        }

        public AdminActivityDetailViewModel(Activity activity)
        {
            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _cts = new CancellationTokenSource();

            // Initialize services
            _activityService = new ActivityService();
            _commentService = new CommentService();
            _userService = new UserService(_activityService);
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299") };

            // Initialize collections
            Participants = new ObservableCollection<User>();
            LikedUsers = new ObservableCollection<User>();
            Comments = new ObservableCollection<Comment>();

            // Initialize commands
            InitializeCommands();

            // Setup auto-refresh timer
            SetupAutoRefreshTimer();

            // Load initial data
            _ = LoadCommentsAsync();
        }

        private void InitializeCommands()
        {
            EditActivityCommand = new RelayCommand(async _ => await ExecuteEditActivityAsync(),
                _ => !IsLoading);

            DeleteActivityCommand = new RelayCommand(async _ => await ExecuteDeleteActivityAsync(),
                _ => !IsLoading);

            LoadParticipantsCommand = new RelayCommand(async _ => await LoadParticipantsAsync(),
                _ => !IsLoading);

            LoadLikedUsersCommand = new RelayCommand(async _ => await LoadLikedUsersAsync(),
                _ => !IsLoading);

            LoadCommentsCommand = new RelayCommand(async _ => await LoadCommentsAsync(),
                _ => !IsLoading);

            RefreshParticipantsCommand = new RelayCommand(async _ => await LoadParticipantsAsync(),
                _ => !IsLoading);

            RefreshLikesCommand = new RelayCommand(async _ => await LoadLikedUsersAsync(),
                _ => !IsLoading);

            RefreshCommentsCommand = new RelayCommand(async _ => await LoadCommentsAsync(),
                _ => !IsLoading);

            RemoveParticipantCommand = new RelayCommand(async param => await RemoveParticipantAsync(param as User),
                param => !IsLoading && param is User);

            RemoveLikeCommand = new RelayCommand(async param => await RemoveLikeAsync(param as User),
                param => !IsLoading && param is User);

            EditCommentCommand = new RelayCommand(async param => await EditCommentAsync(param as Comment),
                param => !IsLoading && param is Comment);

            DeleteCommentCommand = new RelayCommand(async param => await DeleteCommentAsync(param as Comment),
                param => !IsLoading && param is Comment);

        }

        private void SetupAutoRefreshTimer()
        {
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // Refresh every 30 seconds
            };
            _autoRefreshTimer.Tick += async (s, e) => await AutoRefreshData();
        }

        private void HandleAutoRefreshToggle()
        {
            if (AutoRefreshEnabled)
            {
                _autoRefreshTimer?.Start();
                System.Diagnostics.Debug.WriteLine("Auto-refresh enabled");
            }
            else
            {
                _autoRefreshTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("Auto-refresh disabled");
            }
        }

        private async Task AutoRefreshData()
        {
            try
            {
                // Chỉ refresh data hiện đang được xem
                await LoadCommentsAsync();

                // Update activity statistics if needed
                await RefreshActivityStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-refresh error: {ex.Message}");
            }
        }

        private async Task RefreshActivityStatistics()
        {
            try
            {
                // Có thể gọi API để cập nhật thống kê activity
                // Ví dụ: participant count, like count
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing activity statistics: {ex.Message}");
            }
        }

        #region Activity Management

        private async Task ExecuteEditActivityAsync()
        {
            try
            {
                // Tạm thời comment out vì EditActivityDialog chưa có
                // var editDialog = new EditActivityDialog(CloneActivity(Activity));
                // if (editDialog.ShowDialog() == true)
                // {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    // var updatedActivity = await _activityService.UpdateActivityAsync(
                    //     Activity.Id, editDialog.Activity);

                    // Activity = updatedActivity;
                    ShowSuccessMessage("Chức năng chỉnh sửa đang được phát triển!");
                }, "Không thể cập nhật hoạt động");
                // }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Lỗi khi chỉnh sửa hoạt động: {ex.Message}");
            }
        }

        private async Task ExecuteDeleteActivityAsync()
        {
            try
            {
                if (MessageBox.Show("Bạn có chắc muốn xóa hoạt động này? Thao tác này không thể hoàn tác.",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await ExecuteWithErrorHandlingAsync(async () =>
                    {
                        await _activityService.DeleteActivityAsync(Activity.Id);
                        ShowSuccessMessage("Xóa hoạt động thành công!");

                        // Đóng cửa sổ và quay về trang quản lý
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var adminActivitiesView = new AdminActivitiesView();
                            adminActivitiesView.Show();

                            // Đóng cửa sổ hiện tại
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window is AdminActivityDetailView)
                                {
                                    window.Close();
                                    break;
                                }
                            }
                        });
                    }, "Không thể xóa hoạt động");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Lỗi khi xóa hoạt động: {ex.Message}");
            }
        }

        #endregion

        #region Participants Management

        private async Task LoadParticipantsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var participantUsers = await GetParticipantUsersAsync(Activity.Id) ?? new List<User>();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Participants.Clear();
                    foreach (var user in participantUsers)
                    {
                        Participants.Add(user);
                    }
                });
            }, "Không thể tải danh sách người tham gia");
        }

        private async Task RemoveParticipantAsync(User user)
        {
            if (user == null) return;

            try
            {
                if (MessageBox.Show($"Bạn có chắc muốn xóa {user.DisplayName} khỏi danh sách tham gia?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await ExecuteWithErrorHandlingAsync(async () =>
                    {
                        var response = await _httpClient.DeleteAsync(
                            $"/api/ActivityManagement/{Activity.Id}/participants/{user.Id}");

                        if (response.IsSuccessStatusCode)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Participants.Remove(user);
                                Activity.ParticipantCount = Math.Max(0, Activity.ParticipantCount - 1);
                            });
                            ShowSuccessMessage("Đã xóa người tham gia thành công!");
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            ShowErrorMessage($"Không thể xóa người tham gia: {errorContent}");
                        }
                    }, "Không thể xóa người tham gia");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Lỗi khi xóa người tham gia: {ex.Message}");
            }
        }

        #endregion

        #region Likes Management

        private async Task LoadLikedUsersAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var likedUsersList = await GetLikedUsersAsync(Activity.Id) ?? new List<User>();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    LikedUsers.Clear();
                    foreach (var user in likedUsersList)
                    {
                        LikedUsers.Add(user);
                    }
                });
            }, "Không thể tải danh sách người thích");
        }

        private async Task RemoveLikeAsync(User user)
        {
            if (user == null) return;

            try
            {
                if (MessageBox.Show($"Bạn có chắc muốn xóa lượt thích của {user.DisplayName}?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await ExecuteWithErrorHandlingAsync(async () =>
                    {
                        var response = await _httpClient.DeleteAsync(
                            $"/api/ActivityManagement/{Activity.Id}/likes/{user.Id}");

                        if (response.IsSuccessStatusCode)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LikedUsers.Remove(user);
                                Activity.LikeCount = Math.Max(0, Activity.LikeCount - 1);
                            });
                            ShowSuccessMessage("Đã xóa lượt thích thành công!");
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            ShowErrorMessage($"Không thể xóa lượt thích: {errorContent}");
                        }
                    }, "Không thể xóa lượt thích");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Lỗi khi xóa lượt thích: {ex.Message}");
            }
        }

        #endregion

        #region Comments Management

        private async Task LoadCommentsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                IsLoadingComments = true;

                var comments = await _commentService.GetCommentsByActivityIdAsync(Activity.Id, _cts.Token) ?? new List<Comment>();

                System.Diagnostics.Debug.WriteLine($"=== ADMIN LOADING COMMENTS ===");
                System.Diagnostics.Debug.WriteLine($"Loaded {comments.Count} comments");

                // Process comments để đảm bảo hiển thị đúng thông tin reply
                foreach (var comment in comments)
                {
                    if (string.IsNullOrEmpty(comment.UserAvatar))
                    {
                        comment.UserAvatar = "/Views/Images/User-icon.png";
                    }

                    if (string.IsNullOrEmpty(comment.UserDisplayName))
                    {
                        comment.UserDisplayName = "Unknown User";
                    }

                    System.Diagnostics.Debug.WriteLine($"Comment: {comment.Id}, User: {comment.UserDisplayName}, IsReply: {comment.IsReply}, ReplyTo: {comment.ReplyToUserName ?? "N/A"}");
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Comments.Clear();
                    // Sắp xếp comments theo thứ tự: Root comments trước, sau đó là replies
                    var sortedComments = OrganizeCommentsForAdmin(comments);
                    foreach (var comment in sortedComments)
                    {
                        Comments.Add(comment);
                    }

                    UpdateCommentsStatistics();
                });
            }, "Không thể tải danh sách bình luận", false);
        }

        private List<Comment> OrganizeCommentsForAdmin(List<Comment> comments)
        {
            var result = new List<Comment>();

            // Tách comments gốc và replies
            var rootComments = comments.Where(c => string.IsNullOrEmpty(c.ParentCommentId))
                                      .OrderBy(c => c.CreatedAt)
                                      .ToList();

            var replies = comments.Where(c => !string.IsNullOrEmpty(c.ParentCommentId))
                                 .OrderBy(c => c.CreatedAt)
                                 .ToList();

            // Thêm từng comment gốc và replies của nó
            foreach (var rootComment in rootComments)
            {
                result.Add(rootComment);

                // Thêm tất cả replies của comment này
                var commentReplies = replies.Where(r => r.ParentCommentId == rootComment.Id)
                                           .OrderBy(r => r.CreatedAt)
                                           .ToList();

                foreach (var reply in commentReplies)
                {
                    result.Add(reply);

                    // Thêm replies của reply này (nested replies)
                    AddNestedReplies(reply.Id, replies, result);
                }
            }

            return result;
        }

        private void AddNestedReplies(string parentId, List<Comment> allReplies, List<Comment> result)
        {
            var nestedReplies = allReplies.Where(r => r.ParentCommentId == parentId)
                                         .OrderBy(r => r.CreatedAt)
                                         .ToList();

            foreach (var nestedReply in nestedReplies)
            {
                if (!result.Contains(nestedReply)) // Tránh duplicate
                {
                    result.Add(nestedReply);
                    // Tiếp tục đệ quy cho replies của nested reply
                    AddNestedReplies(nestedReply.Id, allReplies, result);
                }
            }
        }

        private void UpdateCommentsStatistics()
        {
            if (Comments == null)
            {
                CommentsCount = 0;
                RootCommentsCount = 0;
                RepliesCount = 0;
                return;
            }

            CommentsCount = Comments.Count;
            RootCommentsCount = Comments.Count(c => string.IsNullOrEmpty(c.ParentCommentId));
            RepliesCount = Comments.Count(c => !string.IsNullOrEmpty(c.ParentCommentId));

            System.Diagnostics.Debug.WriteLine($"Comments Statistics - Total: {CommentsCount}, Root: {RootCommentsCount}, Replies: {RepliesCount}");
        }

        private async Task EditCommentAsync(Comment comment)
        {
            if (comment == null) return;

            try
            {
                // Tạm thời comment out vì EditCommentDialog chưa có
                // var editDialog = new EditCommentDialog(comment.Content);
                // if (editDialog.ShowDialog() == true)
                // {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    // var updatedComment = await _commentService.UpdateCommentAsync(
                    //     comment.Id, editDialog.NewContent, _cts.Token);

                    ShowSuccessMessage("Chức năng chỉnh sửa bình luận đang được phát triển!");
                }, "Không thể cập nhật bình luận");
                // }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Lỗi khi chỉnh sửa bình luận: {ex.Message}");
            }
        }

        private async Task DeleteCommentAsync(Comment comment)
        {
            if (comment == null) return;

            try
            {
                var isRootComment = string.IsNullOrEmpty(comment.ParentCommentId);
                var repliesCount = Comments.Count(c => c.ParentCommentId == comment.Id);

                string confirmMessage;
                if (isRootComment && repliesCount > 0)
                {
                    confirmMessage = $"Bạn có chắc muốn xóa bình luận của {comment.UserDisplayName}?\n" +
                                   $"Thao tác này sẽ xóa luôn {repliesCount} phản hồi.";
                }
                else
                {
                    confirmMessage = $"Bạn có chắc muốn xóa bình luận của {comment.UserDisplayName}?";
                }

                if (MessageBox.Show(confirmMessage, "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await ExecuteWithErrorHandlingAsync(async () =>
                    {
                        var success = await _commentService.DeleteCommentAsync(comment.Id, _cts.Token);

                        if (success)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Nếu là root comment, xóa tất cả replies
                                if (isRootComment)
                                {
                                    var commentsToRemove = Comments.Where(c =>
                                        c.Id == comment.Id ||
                                        c.ParentCommentId == comment.Id ||
                                        IsChildOfComment(c, comment.Id)).ToList();

                                    foreach (var commentToRemove in commentsToRemove)
                                    {
                                        Comments.Remove(commentToRemove);
                                    }

                                    ShowSuccessMessage($"Đã xóa bình luận và {commentsToRemove.Count - 1} phản hồi!");
                                }
                                else
                                {
                                    Comments.Remove(comment);
                                    ShowSuccessMessage("Xóa bình luận thành công!");
                                }

                                UpdateCommentsStatistics();
                            });
                        }
                        else
                        {
                            ShowErrorMessage("Không thể xóa bình luận.");
                        }
                    }, "Không thể xóa bình luận");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Lỗi khi xóa bình luận: {ex.Message}");
            }
        }

        private bool IsChildOfComment(Comment comment, string parentId)
        {
            if (string.IsNullOrEmpty(comment.ParentCommentId))
                return false;

            if (comment.ParentCommentId == parentId)
                return true;

            var parentComment = Comments.FirstOrDefault(c => c.Id == comment.ParentCommentId);
            if (parentComment != null)
            {
                return IsChildOfComment(parentComment, parentId);
            }

            return false;
        }

        private Comment FindRootComment(Comment comment)
        {
            if (string.IsNullOrEmpty(comment.ParentCommentId))
                return comment;

            var parentComment = Comments.FirstOrDefault(c => c.Id == comment.ParentCommentId);
            if (parentComment != null)
            {
                return FindRootComment(parentComment);
            }

            return comment; // Fallback nếu không tìm thấy parent
        }

        private void CollectThreadComments(string rootCommentId, List<Comment> threadComments)
        {
            var replies = Comments.Where(c => c.ParentCommentId == rootCommentId)
                                 .OrderBy(c => c.CreatedAt)
                                 .ToList();

            foreach (var reply in replies)
            {
                threadComments.Add(reply);
                // Đệ quy để lấy replies của reply
                CollectThreadComments(reply.Id, threadComments);
            }
        }

        #endregion

        #region Helper Methods

        private async Task<List<User>> GetParticipantUsersAsync(string activityId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/ActivityManagement/{activityId}/participants");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var participantData = JsonSerializer.Deserialize<JsonElement[]>(json, options);
                    var users = new List<User>();

                    if (participantData != null)
                    {
                        foreach (var data in participantData)
                        {
                            try
                            {
                                var user = new User
                                {
                                    Id = data.GetProperty("id").GetString() ?? "",
                                    Username = data.GetProperty("username").GetString() ?? "",
                                    DisplayName = data.GetProperty("displayName").GetString() ?? "",
                                    Email = data.GetProperty("email").GetString() ?? "",
                                    AvatarUrl = data.TryGetProperty("avatarUrl", out var avatar) ? avatar.GetString() : "",
                                };
                                users.Add(user);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error parsing participant: {ex.Message}");
                            }
                        }
                    }

                    return users;
                }

                return new List<User>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting participants: {ex.Message}");
                return new List<User>();
            }
        }

        private async Task<List<User>> GetLikedUsersAsync(string activityId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/ActivityManagement/{activityId}/likes");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Parse as array of like objects
                    var likeData = JsonSerializer.Deserialize<JsonElement[]>(json, options);
                    var users = new List<User>();

                    if (likeData != null)
                    {
                        foreach (var data in likeData)
                        {
                            try
                            {
                                var user = new User
                                {
                                    Id = data.GetProperty("id").GetString() ?? "",
                                    Username = data.GetProperty("username").GetString() ?? "",
                                    DisplayName = data.GetProperty("displayName").GetString() ?? "",
                                    Email = data.GetProperty("email").GetString() ?? "",
                                    AvatarUrl = data.TryGetProperty("avatarUrl", out var avatar) ? avatar.GetString() : "",
                                };
                                users.Add(user);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error parsing liked user: {ex.Message}");
                            }
                        }
                    }

                    return users;
                }

                return new List<User>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting liked users: {ex.Message}");
                return new List<User>();
            }
        }

        private Activity CloneActivity(Activity source)
        {
            return new Activity
            {
                Id = source.Id,
                Title = source.Title,
                Description = source.Description,
                Type = source.Type,
                Date = source.Date,
                ImgUrl = source.ImgUrl,
                CreatedAt = source.CreatedAt,
                Status = source.Status,
                ParticipantCount = source.ParticipantCount,
                LikeCount = source.LikeCount
            };
        }

        private async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string defaultErrorMessage, bool showLoading = true)
        {
            try
            {
                if (showLoading) IsLoading = true;
                ErrorMessage = null;
                await action();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorMessage = $"{defaultErrorMessage}: {ex.Message}";
                    ShowErrorMessage(ErrorMessage);
                });
            }
            finally
            {
                if (showLoading) IsLoading = false;
                IsLoadingComments = false;
            }
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void Cleanup()
        {
            try
            {
                _autoRefreshTimer?.Stop();
                _cts?.Cancel();
                _cts?.Dispose();
                _httpClient?.Dispose();
                Participants?.Clear();
                LikedUsers?.Clear();
                Comments?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}