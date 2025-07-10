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

namespace DoanKhoaClient.ViewModels
{
    public class AdminActivityDetailViewModel : INotifyPropertyChanged
    {
        private readonly ActivityService _activityService;
        private readonly CommentService _commentService;
        private readonly UserService _userService;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;

        private Activity _activity;
        private ObservableCollection<User> _participants;
        private ObservableCollection<User> _likedUsers;
        private ObservableCollection<Comment> _comments;
        private int _commentsCount;
        private bool _isLoading;
        private string _errorMessage;

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
            }
        }

        public int CommentsCount
        {
            get => _commentsCount;
            set
            {
                _commentsCount = value;
                OnPropertyChanged();
            }
        }

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

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand EditActivityCommand { get; }
        public ICommand DeleteActivityCommand { get; }
        public ICommand LoadParticipantsCommand { get; }
        public ICommand LoadLikedUsersCommand { get; }
        public ICommand LoadCommentsCommand { get; }
        public ICommand RefreshParticipantsCommand { get; }
        public ICommand RefreshLikesCommand { get; }
        public ICommand RefreshCommentsCommand { get; }
        public ICommand RemoveParticipantCommand { get; }
        public ICommand RemoveLikeCommand { get; }
        public ICommand EditCommentCommand { get; }
        public ICommand DeleteCommentCommand { get; }

        public AdminActivityDetailViewModel(Activity activity)
        {
            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _cts = new CancellationTokenSource();

            // Initialize services
            _activityService = new ActivityService();
            _commentService = new CommentService();
            _userService = new UserService();
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299") };

            // Initialize collections
            Participants = new ObservableCollection<User>();
            LikedUsers = new ObservableCollection<User>();
            Comments = new ObservableCollection<Comment>();

            // Initialize commands
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

            // Load initial data
            _ = LoadCommentsAsync();
        }

        #region Activity Management

        private async Task ExecuteEditActivityAsync()
        {
            try
            {
                var editDialog = new EditActivityDialog(CloneActivity(Activity));
                if (editDialog.ShowDialog() == true)
                {
                    await ExecuteWithErrorHandlingAsync(async () =>
                    {
                        var updatedActivity = await _activityService.UpdateActivityAsync(
                            Activity.Id, editDialog.Activity);

                        Activity = updatedActivity;
                        ShowSuccessMessage("Cập nhật hoạt động thành công!");
                    }, "Không thể cập nhật hoạt động");
                }
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
                var comments = await _commentService.GetCommentsByActivityIdAsync(Activity.Id, _cts.Token) ?? new List<Comment>();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Comments.Clear();
                    foreach (var comment in comments.OrderBy(c => c.CreatedAt))
                    {
                        Comments.Add(comment);
                    }
                    CommentsCount = Comments.Count;
                });
            }, "Không thể tải danh sách bình luận");
        }

        private async Task EditCommentAsync(Comment comment)
        {
            if (comment == null) return;

            try
            {
                var editDialog = new EditCommentDialog(comment.Content);
                if (editDialog.ShowDialog() == true)
                {
                    await ExecuteWithErrorHandlingAsync(async () =>
                    {
                        var updatedComment = await _commentService.UpdateCommentAsync(
                            comment.Id, editDialog.NewContent, _cts.Token);

                        if (updatedComment != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var index = Comments.IndexOf(comment);
                                if (index >= 0)
                                {
                                    Comments[index] = updatedComment;
                                }
                            });
                            ShowSuccessMessage("Cập nhật bình luận thành công!");
                        }
                        else
                        {
                            ShowErrorMessage("Không thể cập nhật bình luận - phản hồi từ server không hợp lệ.");
                        }
                    }, "Không thể cập nhật bình luận");
                }
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
                if (MessageBox.Show($"Bạn có chắc muốn xóa bình luận của {comment.UserDisplayName}?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await ExecuteWithErrorHandlingAsync(async () =>
                    {
                        var success = await _commentService.DeleteCommentAsync(comment.Id, _cts.Token);

                        if (success)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Comments.Remove(comment);
                                CommentsCount = Comments.Count;
                            });
                            ShowSuccessMessage("Xóa bình luận thành công!");
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

                    // Parse as array of participant objects
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
                                    //Position = data.TryGetProperty("position", out var pos) ? pos.GetString() : ""
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
                                    //Position = data.TryGetProperty("position", out var pos) ? pos.GetString() : ""
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

        private async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string defaultErrorMessage)
        {
            try
            {
                IsLoading = true;
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
                IsLoading = false;
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