using DoanKhoaClient.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.ConstrainedExecution;

namespace DoanKhoaClient.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private static User _currentUser;
        private static Dictionary<string, bool> _participatedActivities = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _likedActivities = new Dictionary<string, bool>();
        private readonly ActivityService _activityService;
        private readonly JsonSerializerOptions _jsonOptions;

        public event EventHandler<UserActivityStatusChangedEventArgs> ActivityStatusChanged;

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
            // Load user's activity statuses when user is set
            _ = LoadUserActivityStatusesAsync();
        }

        public string GetCurrentUserId()
        {
            return _currentUser?.Id ?? "guest-user";
        }

        public bool IsActivityParticipated(string activityId)
        {
            return _participatedActivities.ContainsKey(activityId) && _participatedActivities[activityId];
        }

        public bool IsActivityLiked(string activityId)
        {
            return _likedActivities.ContainsKey(activityId) && _likedActivities[activityId];
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

        public async Task RefreshUserActivityStatusesAsync()
        {
            await LoadUserActivityStatusesAsync();
        }

        // Existing methods
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
            var response = await _httpClient.PutAsJsonAsync($"user/{userId}", updatedUser);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<User>();
            else
                throw new Exception(await response.Content.ReadAsStringAsync());
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
}