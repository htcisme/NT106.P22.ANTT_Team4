using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Text;

namespace DoanKhoaClient.Services
{
    public class CommentService
    {
        private readonly HttpClient _httpClient;
        private readonly string BaseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public CommentService(string baseUrl = "http://localhost:5299/api/")
        {
            BaseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            LogRequestData($"CommentService initialized with base URL: {baseUrl}");
        }

        // IMPROVED: Better connection test
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                LogRequestData($"Testing connection to: {BaseUrl}comment/test");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                // First, test if server is running at all
                var pingResponse = await _httpClient.GetAsync("", cts.Token);
                LogResponseData($"Server ping response: {pingResponse.StatusCode}");

                // Then test comment endpoint
                var response = await _httpClient.GetAsync("comment/test", cts.Token);
                LogResponseData($"Test connection response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    LogResponseData("Connection test successful");
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    LogResponseData("Comment API endpoint not found - API may not be implemented");
                    return false;
                }
                else
                {
                    LogResponseData($"Server responded but with error: {response.StatusCode}");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                LogResponseData($"Connection test failed - HttpRequestException: {ex.Message}");

                // Check if it's a connection refused error
                if (ex.Message.Contains("refused") || ex.Message.Contains("No connection"))
                {
                    LogResponseData("Server appears to be offline");
                    return false;
                }
                return false;
            }
            catch (TaskCanceledException ex)
            {
                LogResponseData($"Connection test timed out: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogResponseData($"Connection test failed - General exception: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Comment>> GetCommentsByActivityIdAsync(string activityId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(activityId))
                throw new ArgumentException("Activity ID cannot be null or empty", nameof(activityId));

            try
            {
                LogRequestData($"Getting comments for activity: {activityId}");

                // IMPROVED: More detailed connection testing
                var isConnected = await TestConnectionAsync();
                if (!isConnected)
                {
                    LogResponseData("Server connection failed, returning empty list");
                    throw new Exception("Không thể kết nối đến server. Vui lòng kiểm tra:\n" +
                                      "1. Server có đang chạy không?\n" +
                                      "2. URL server có đúng không? (" + BaseUrl + ")\n" +
                                      "3. Comment API đã được implement chưa?");
                }

                // Get current user ID for proper status
                var currentUserId = DoanKhoaClient.Properties.Settings.Default.CurrentUserId;
                var url = string.IsNullOrEmpty(currentUserId)
                    ? $"comment/activity/{activityId}"
                    : $"comment/activity/{activityId}?userId={currentUserId}";

                return await SendRequestAsync<List<Comment>>(
                    () => _httpClient.GetAsync(url, cancellationToken),
                    "Lỗi khi lấy danh sách bình luận"
                );
            }
            catch (Exception ex)
            {
                LogResponseData($"GetCommentsByActivityIdAsync failed: {ex.Message}");
                // Re-throw with original error instead of returning empty list
                throw;
            }
        }

        public async Task<Comment> CreateCommentAsync(CreateCommentRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Comment request cannot be null");

            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ArgumentException("Comment content cannot be empty", nameof(request.Content));

            try
            {
                LogRequestData("=== Starting CreateCommentAsync ===");

                // IMPROVED: Better validation with detailed error messages
                await ValidateCommentRequest(request);

                // Test connection with better error reporting
                LogRequestData("Testing server connection...");
                var isConnected = await TestConnectionAsync();
                if (!isConnected)
                {
                    throw new Exception("Server không phản hồi. Vui lòng kiểm tra:\n" +
                                      "1. Server đang chạy tại: " + BaseUrl + "\n" +
                                      "2. Comment API đã được implement\n" +
                                      "3. Database đã được cấu hình đúng");
                }
                LogRequestData("Server connection OK");

                // Create request object with proper null handling
                var requestObject = new
                {
                    ActivityId = request.ActivityId,
                    UserId = request.UserId,
                    Content = request.Content.Trim(),
                    ParentCommentId = string.IsNullOrWhiteSpace(request.ParentCommentId) ? null : request.ParentCommentId
                };

                string jsonRequest = JsonSerializer.Serialize(requestObject, _jsonOptions);
                LogRequestData($"JSON Request: {jsonRequest}");

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                LogRequestData("Sending request to server...");
                var result = await SendRequestAsync<Comment>(
                    () => _httpClient.PostAsync("comment", content, cancellationToken),
                    "Lỗi khi tạo bình luận"
                );

                LogRequestData($"Request completed successfully, Comment ID: {result?.Id}");
                return result;
            }
            catch (Exception ex)
            {
                LogResponseData($"CreateCommentAsync failed: {ex.Message}");
                throw; // Re-throw the original exception
            }
        }

        // IMPROVED: Better validation method
        private async Task ValidateCommentRequest(CreateCommentRequest request)
        {
            // Validate and set UserId if needed
            if (string.IsNullOrEmpty(request.UserId))
            {
                var settings = DoanKhoaClient.Properties.Settings.Default;
                request.UserId = settings.CurrentUserId;

                if (string.IsNullOrEmpty(request.UserId))
                {
                    request.UserId = "676b4e0e2d5a8b1234567890";
                    LogRequestData($"Using fallback user ID: {request.UserId}");
                }
            }

            // Validate ObjectId formats
            if (!IsValidObjectId(request.ActivityId))
            {
                throw new ArgumentException($"Invalid ActivityId format: {request.ActivityId}");
            }

            if (!IsValidObjectId(request.UserId))
            {
                throw new ArgumentException($"Invalid UserId format: {request.UserId}");
            }

            // Handle ParentCommentId
            if (!string.IsNullOrWhiteSpace(request.ParentCommentId))
            {
                if (!IsValidObjectId(request.ParentCommentId))
                {
                    LogRequestData($"Invalid ParentCommentId format: {request.ParentCommentId}, setting to null");
                    request.ParentCommentId = null;
                }
            }

            // Validate content length
            if (request.Content.Length > 1000)
            {
                throw new ArgumentException("Nội dung bình luận quá dài (tối đa 1000 ký tự)");
            }

            LogRequestData($"Validation successful - ActivityId: {request.ActivityId}, UserId: {request.UserId}");
        }

        // Helper method to validate ObjectId
        private bool IsValidObjectId(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
                return false;

            try
            {
                return MongoDB.Bson.ObjectId.TryParse(objectId, out _);
            }
            catch
            {
                return false;
            }
        }

        // Rest of the methods remain the same...
        public async Task<Comment> UpdateCommentAsync(string commentId, string newContent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(commentId))
                throw new ArgumentException("Comment ID cannot be null or empty", nameof(commentId));

            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Comment content cannot be empty", nameof(newContent));

            try
            {
                var updateRequest = new { Content = newContent };
                string jsonRequest = JsonSerializer.Serialize(updateRequest, _jsonOptions);
                LogRequestData($"Update Comment Request: {jsonRequest}");

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                return await SendRequestAsync<Comment>(
                    () => _httpClient.PutAsync($"comment/{commentId}", content, cancellationToken),
                    "Lỗi khi cập nhật bình luận"
                );
            }
            catch (Exception ex)
            {
                LogResponseData($"UpdateCommentAsync failed: {ex.Message}");
                throw new Exception($"Không thể cập nhật bình luận: {ex.Message}");
            }
        }

        public async Task DeleteCommentAsync(string commentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(commentId))
                throw new ArgumentException("Comment ID cannot be null or empty", nameof(commentId));

            try
            {
                var response = await _httpClient.DeleteAsync($"comment/{commentId}", cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponseData($"Delete Comment Response Status: {response.StatusCode}");
                LogResponseData($"Delete Comment Response Content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Máy chủ trả về lỗi: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                LogResponseData($"DeleteCommentAsync failed: {ex.Message}");
                throw new Exception($"Lỗi khi xóa bình luận: {ex.Message}", ex);
            }
        }

        public async Task<bool> ToggleCommentLikeAsync(string commentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(commentId))
                throw new ArgumentException("Comment ID cannot be null or empty", nameof(commentId));

            try
            {
                // Get current user ID
                var currentUserId = DoanKhoaClient.Properties.Settings.Default.CurrentUserId;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    currentUserId = "676b4e0e2d5a8b1234567890";
                }

                LogRequestData($"Toggling like for comment {commentId} by user {currentUserId}");

                var response = await _httpClient.PostAsync($"comment/{commentId}/like?userId={currentUserId}", null, cancellationToken);

                LogResponseData($"Toggle like response: {response.StatusCode}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogResponseData($"Error in ToggleCommentLikeAsync: {ex.Message}");
                return false; // Return false instead of throwing to prevent UI crash
            }
        }

        public async Task<Dictionary<string, bool>> GetUserCommentStatusAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            try
            {
                var response = await _httpClient.GetAsync($"comment/user-status/{userId}", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(cancellationToken: cancellationToken)
                           ?? new Dictionary<string, bool>();
                }

                LogResponseData($"Failed to get user comment status: {response.StatusCode}");
                return new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                LogResponseData($"GetUserCommentStatusAsync failed: {ex.Message}");
                return new Dictionary<string, bool>();
            }
        }

        // IMPROVED: Better error handling in SendRequestAsync
        private async Task<T> SendRequestAsync<T>(Func<Task<HttpResponseMessage>> requestFunc, string errorMessage)
        {
            try
            {
                var response = await requestFunc();
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponseData($"Response Status: {response.StatusCode}");
                LogResponseData($"Response Content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    // Handle specific error cases with better user messages
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new Exception("API endpoint không tồn tại. Vui lòng kiểm tra:\n" +
                                          "1. Server đã implement Comment API chưa?\n" +
                                          "2. CommentController đã được thêm vào project chưa?\n" +
                                          "3. Route mapping có đúng không?");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        throw new Exception("Server gặp lỗi nội bộ. Vui lòng kiểm tra:\n" +
                                          "1. Database connection\n" +
                                          "2. MongoDB collections đã được tạo chưa?\n" +
                                          "3. Server logs để biết chi tiết lỗi");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new Exception($"Dữ liệu gửi lên không hợp lệ:\n{responseContent}");
                    }
                    else
                    {
                        throw new HttpRequestException($"Máy chủ trả về lỗi: {response.StatusCode} - {responseContent}");
                    }
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    if (typeof(T) == typeof(object))
                    {
                        return default;
                    }
                    throw new JsonException("Server trả về dữ liệu rỗng");
                }

                try
                {
                    if (typeof(T) == typeof(object))
                    {
                        return default;
                    }
                    return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"JSON không hợp lệ: {responseContent}");
                    LogResponseData($"JSON không hợp lệ: {ex.Message}");

                    if (responseContent.Contains("<!DOCTYPE html>") || responseContent.Contains("<html"))
                    {
                        throw new Exception("Server trả về HTML thay vì JSON. Comment API chưa được implement hoặc có lỗi trong routing.");
                    }

                    throw new Exception($"Lỗi xử lý dữ liệu từ máy chủ: {ex.Message}.\nDữ liệu trả về: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("No connection could be made") || ex.Message.Contains("refused"))
                {
                    throw new Exception($"Không thể kết nối đến server tại {BaseUrl}.\nVui lòng kiểm tra:\n" +
                                      "1. Server có đang chạy không?\n" +
                                      "2. Port 5299 có bị chặn không?\n" +
                                      "3. URL có đúng không?");
                }
                throw new Exception($"Lỗi kết nối: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Yêu cầu đã hết thời gian chờ. Server có thể đang quá tải hoặc không phản hồi.");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Lỗi xử lý dữ liệu từ máy chủ: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"{errorMessage}: {ex.Message}");
            }
        }

        private void LogRequestData(string message)
        {
            Debug.WriteLine($"[CommentService Request] {message}");
            Console.WriteLine($"[CommentService Request] {message}");
        }

        private void LogResponseData(string message)
        {
            Debug.WriteLine($"[CommentService Response] {message}");
            Console.WriteLine($"[CommentService Response] {message}");
        }
    }
}