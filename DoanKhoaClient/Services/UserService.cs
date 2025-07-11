using DoanKhoaClient.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.ConstrainedExecution;
using System.Linq;
using System.Windows;

namespace DoanKhoaClient.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private static User _currentUser;
        private static Dictionary<string, bool> _participatedActivities = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _likedActivities = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _likedComments = new Dictionary<string, bool>();
        private readonly ActivityService _activityService;
        private readonly JsonSerializerOptions _jsonOptions;

        public event EventHandler<UserActivityStatusChangedEventArgs> ActivityStatusChanged;
        public event EventHandler<UserCommentStatusChangedEventArgs> CommentStatusChanged;

        public UserService(ActivityService activityService = null)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };
            _activityService = activityService ?? new ActivityService();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // If we already have a current user in the App properties, set it
            if (App.Current.Properties.Contains("CurrentUser") &&
                App.Current.Properties["CurrentUser"] is User currentUser)
            {
                SetCurrentUser(currentUser);
            }
        }

        public User CurrentUser => _currentUser;

        public void SetCurrentUser(User user)
        {
            _currentUser = user;

            // Store in App properties for global access
            App.Current.Properties["CurrentUser"] = user;

            // Save to settings for persistence
            try
            {
                Properties.Settings.Default.CurrentUserId = user?.Id ?? "";
                Properties.Settings.Default.CurrentUserDisplayName = user?.DisplayName ?? "";
                Properties.Settings.Default.CurrentUserAvatar = user?.AvatarUrl ?? "";
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user settings: {ex.Message}");
            }

            // Load user's activity and comment statuses when user is set
            _ = LoadUserActivityStatusesAsync();
            _ = LoadUserCommentStatusesAsync();
        }

        public string GetCurrentUserId()
        {
            if (_currentUser != null)
                return _currentUser.Id;

            // Fallback to settings
            try
            {
                var savedUserId = Properties.Settings.Default.CurrentUserId;
                return !string.IsNullOrEmpty(savedUserId) ? savedUserId : "guest-user";
            }
            catch
            {
                return "guest-user";
            }
        }

        public string GetCurrentUserDisplayName()
        {
            if (_currentUser != null)
                return _currentUser.DisplayName ?? _currentUser.Username ?? "Unknown User";

            // Fallback to settings
            try
            {
                var savedDisplayName = Properties.Settings.Default.CurrentUserDisplayName;
                return !string.IsNullOrEmpty(savedDisplayName) ? savedDisplayName : "Unknown User";
            }
            catch
            {
                return "Unknown User";
            }
        }

        public string GetCurrentUserAvatar()
        {
            if (_currentUser != null)
                return _currentUser.AvatarUrl ?? "";

            // Fallback to settings
            try
            {
                return Properties.Settings.Default.CurrentUserAvatar ?? "";
            }
            catch
            {
                return "";
            }
        }

        public void ClearCurrentUser()
        {
            _currentUser = null;
            _participatedActivities.Clear();
            _likedActivities.Clear();
            _likedComments.Clear();

            // Clear from App properties
            if (App.Current.Properties.Contains("CurrentUser"))
            {
                App.Current.Properties.Remove("CurrentUser");
            }

            // Clear from settings
            try
            {
                Properties.Settings.Default.CurrentUserId = "";
                Properties.Settings.Default.CurrentUserDisplayName = "";
                Properties.Settings.Default.CurrentUserAvatar = "";
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing user settings: {ex.Message}");
            }
        }

        // Activity-related methods
        public bool IsActivityParticipated(string activityId)
        {
            return _participatedActivities.ContainsKey(activityId) && _participatedActivities[activityId];
        }

        public bool IsActivityLiked(string activityId)
        {
            return _likedActivities.ContainsKey(activityId) && _likedActivities[activityId];
        }

        // Comment-related methods
        public bool IsCommentLiked(string commentId)
        {
            return _likedComments.ContainsKey(commentId) && _likedComments[commentId];
        }

        public async Task<bool> ToggleActivityParticipationAsync(string activityId)
        {
            if (string.IsNullOrEmpty(activityId) || _currentUser == null)
                return false;

            try
            {
                var result = await _activityService.ToggleParticipationAsync(activityId, _currentUser.Id);
                if (result)
                {
                    // Update local cache
                    bool newStatus = !IsActivityParticipated(activityId);
                    _participatedActivities[activityId] = newStatus;

                    // Notify listeners
                    ActivityStatusChanged?.Invoke(this, new UserActivityStatusChangedEventArgs
                    {
                        ActivityId = activityId,
                        IsParticipated = newStatus,
                        StatusType = ActivityStatusType.Participation
                    });

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling participation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleActivityLikeAsync(string activityId)
        {
            if (string.IsNullOrEmpty(activityId) || _currentUser == null)
                return false;
            try
            {
                var result = await _activityService.ToggleLikeAsync(activityId, _currentUser.Id);
                if (result)
                {
                    // Update local cache
                    bool newStatus = !IsActivityLiked(activityId);
                    _likedActivities[activityId] = newStatus;

                    // Notify listeners
                    ActivityStatusChanged?.Invoke(this, new UserActivityStatusChangedEventArgs
                    {
                        ActivityId = activityId,
                        IsLiked = newStatus,
                        StatusType = ActivityStatusType.Like
                    });

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling like: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleCommentLikeAsync(string commentId)
        {
            if (string.IsNullOrEmpty(commentId) || _currentUser == null)
                return false;

            try
            {
                var commentService = new CommentService();
                var result = await commentService.ToggleCommentLikeAsync(commentId);

                if (result)
                {
                    // Update local cache
                    bool newStatus = !IsCommentLiked(commentId);
                    _likedComments[commentId] = newStatus;

                    // Notify listeners
                    CommentStatusChanged?.Invoke(this, new UserCommentStatusChangedEventArgs
                    {
                        CommentId = commentId,
                        IsLiked = newStatus
                    });

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling comment like: {ex.Message}");
                return false;
            }
        }

        private async Task LoadUserActivityStatusesAsync()
        {
            if (_currentUser == null)
                return;

            try
            {
                var statuses = await _activityService.GetUserActivityStatusAsync(_currentUser.Id);
                _participatedActivities.Clear();
                _likedActivities.Clear();

                foreach (var status in statuses)
                {
                    // Format from API is expected to be "activityId:participation" and "activityId:like"
                    if (status.Key.EndsWith(":participation"))
                    {
                        string activityId = status.Key.Split(':')[0];
                        _participatedActivities[activityId] = status.Value;
                    }
                    else if (status.Key.EndsWith(":like"))
                    {
                        string activityId = status.Key.Split(':')[0];
                        _likedActivities[activityId] = status.Value;
                    }
                }

                Debug.WriteLine($"Loaded {_participatedActivities.Count} participation statuses and {_likedActivities.Count} like statuses");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load user activity statuses: {ex.Message}");
            }
        }

        private async Task LoadUserCommentStatusesAsync()
        {
            if (_currentUser == null)
                return;

            try
            {
                var commentService = new CommentService();
                var statuses = await commentService.GetUserCommentStatusesAsync(_currentUser.Id);
                _likedComments.Clear();

                foreach (var status in statuses)
                {
                    // Format from API is expected to be "commentId:like"
                    if (status.Key.EndsWith(":like"))
                    {
                        string commentId = status.Key.Split(':')[0];
                        _likedComments[commentId] = status.Value;
                    }
                }

                Debug.WriteLine($"Loaded {_likedComments.Count} comment like statuses");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load user comment statuses: {ex.Message}");
            }
        }


        public async Task RefreshUserActivityStatusesAsync()
        {
            await LoadUserActivityStatusesAsync();
        }

        public async Task RefreshUserCommentStatusesAsync()
        {
            await LoadUserCommentStatusesAsync();
        }

        // Existing methods for user management
        public async Task<User> GetCurrentUserAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("user/current");

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<User>();
                    // Set as current user and load activity statuses
                    SetCurrentUser(user);
                    return user;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error getting current user: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get current user: {ex.Message}", ex);
            }
        }

        public async Task<Conversation> CreatePrivateConversationAsync(string userId)
        {
            try
            {
                // Get current user ID from the app properties
                if (!App.Current.Properties.Contains("CurrentUser") ||
                    !(App.Current.Properties["CurrentUser"] is User currentUser))
                {
                    throw new Exception("User is not logged in");
                }

                var request = new { UserId = userId, CurrentUserId = currentUser.Id };
                var response = await _httpClient.PostAsJsonAsync("conversations/private", request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Conversation>();
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error creating conversation: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create conversation: {ex.Message}", ex);
            }
        }

        public async Task<List<User>> SearchUsersAsync(string query)
        {
            try
            {
                var response = await _httpClient.GetAsync($"user/search?query={Uri.EscapeDataString(query)}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<User>>();
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error searching users: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"User search failed: {ex.Message}", ex);
            }
        }
        public async Task<dynamic> BatchUpdateUsersAsync(List<string> userIds, object updates, string adminCode = null)
        {
            try
            {
                var request = new
                {
                    UserIds = userIds,
                    Updates = updates,
                    AdminCode = adminCode
                };

                Debug.WriteLine($"Batch updating users: {JsonSerializer.Serialize(request)}");

                var response = await _httpClient.PutAsJsonAsync("user/batch-update", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Batch update response: {response.StatusCode} - {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<dynamic>(responseContent);
                    return result;
                }
                else
                {
                    throw new Exception($"Lỗi cập nhật hàng loạt: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Network error: {ex.Message}");
                throw new Exception("Lỗi kết nối tới server. Vui lòng kiểm tra kết nối mạng.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Batch update error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            try
            {
                var request = new
                {
                    UserId = userId,
                    NewPassword = newPassword
                };

                var response = await _httpClient.PostAsJsonAsync($"user/{userId}/reset-password", request);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Password reset successfully for user {userId}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Failed to reset password for user {userId}: {errorContent}");
                    throw new Exception($"Lỗi đặt lại mật khẩu: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Network error when resetting password: {ex.Message}");
                throw new Exception("Lỗi kết nối tới server. Vui lòng kiểm tra kết nối mạng.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting password: {ex.Message}");
                throw;
            }
        }

        public async Task<List<User>> GetUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("user/all");
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"GetUsersAsync Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var users = JsonSerializer.Deserialize<List<User>>(responseContent, _jsonOptions);
                    Debug.WriteLine($"Parsed Users: {string.Join(", ", users.Select(u => $"{u.Username}: {u.Role}"))}");
                    return users;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error getting users: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetUsersAsync Error: {ex}");
                throw new Exception($"Failed to get users: {ex.Message}", ex);
            }
        }

        public async Task<User> UpdateUserAsync(string userId, User updatedUser)
        {
            try
            {
                Debug.WriteLine($"Updating user {userId} with data: {JsonSerializer.Serialize(updatedUser)}");

                var response = await _httpClient.PutAsJsonAsync($"user/{userId}", updatedUser);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Update response: {response.StatusCode} - {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<User>();
                    Debug.WriteLine($"Updated user successfully: {JsonSerializer.Serialize(result)}");
                    return result;
                }
                else
                {
                    Debug.WriteLine($"Update failed: {response.StatusCode} - {responseContent}");
                    throw new Exception($"Lỗi cập nhật người dùng: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Network error: {ex.Message}");
                throw new Exception("Lỗi kết nối tới server. Vui lòng kiểm tra kết nối mạng.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error: {ex.Message}");
                throw new Exception($"Lỗi không mong muốn: {ex.Message}");
            }
        }

        public async Task<User> UpdateUserToAdminAsync(string userId, object updatedUserWithAdminCode)
        {
            var response = await _httpClient.PutAsJsonAsync($"user/{userId}/promote-to-admin", updatedUserWithAdminCode);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<User>();
            else
                throw new Exception(await response.Content.ReadAsStringAsync());
        }

        public async Task DeleteUserAsync(string userId)
        {
            try
            {
                // Đảm bảo gửi token nếu có người dùng hiện tại đăng nhập
                if (_currentUser != null)
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer");
                }

                var response = await _httpClient.DeleteAsync($"user/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error deleting user {userId}: {response.StatusCode} - {errorContent}");

                    // Kiểm tra các mã trạng thái cụ thể để đưa ra thông báo lỗi có ý nghĩa
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        throw new Exception("Không có quyền truy cập. Vui lòng đăng nhập lại.");
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        throw new Exception("Bạn không có quyền xóa người dùng này.");
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new Exception("Không tìm thấy người dùng.");
                    else
                        throw new Exception($"Lỗi: {errorContent}");
                }
                else
                {
                    Debug.WriteLine($"User {userId} deleted successfully.");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Network error when deleting user: {ex.Message}");
                throw new Exception("Lỗi kết nối tới server. Vui lòng kiểm tra kết nối mạng.");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                // Re-throw các Exception đã được customize ở trên
                throw;
            }
        }

        public async Task<User> CreateUserAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("user/register", request);
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null && !string.IsNullOrEmpty(authResponse.Id))
                {
                    return new User
                    {
                        Id = authResponse.Id,
                        Username = authResponse.Username,
                        DisplayName = authResponse.DisplayName,
                        Email = authResponse.Email,
                        AvatarUrl = authResponse.AvatarUrl,
                        Role = authResponse.Role,
                    };
                }
                throw new Exception(authResponse?.Message ?? "Không thể tạo thành viên mới.");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi tạo thành viên: {error}");
            }
        }
    }

    public enum ActivityStatusType
    {
        Participation,
        Like
    }

    public class UserActivityStatusChangedEventArgs : EventArgs
    {
        public string ActivityId { get; set; }
        public bool IsParticipated { get; set; }
        public bool IsLiked { get; set; }
        public ActivityStatusType StatusType { get; set; }
    }

    public class UserCommentStatusChangedEventArgs : EventArgs
    {
        public string CommentId { get; set; }
        public bool IsLiked { get; set; }
    }
}