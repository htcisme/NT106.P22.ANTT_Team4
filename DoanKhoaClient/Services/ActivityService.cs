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
    public class ActivityService
    {
        private readonly HttpClient _httpClient;
        private readonly string BaseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public ActivityService(string baseUrl = "http://localhost:5299/api/")
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
        }

        public async Task<List<DoanKhoaClient.Models.Activity>> GetActivitiesAsync(CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<List<DoanKhoaClient.Models.Activity>>(
                () => _httpClient.GetAsync("activity", cancellationToken),
                "Lỗi khi lấy danh sách hoạt động"
            );
        }

        public async Task<DoanKhoaClient.Models.Activity> CreateActivityAsync(DoanKhoaClient.Models.Activity activity, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[DEBUG] Activity.Id = {activity.Id}");

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity), "Activity cannot be null");
            }

            var activityToSend = new DoanKhoaClient.Models.Activity
            {
                Title = activity.Title,
                Description = activity.Description,
                Type = activity.Type,
                Date = activity.Date,
                ImgUrl = activity.ImgUrl,
                CreatedAt = activity.CreatedAt,
                Status = activity.Status
            };

            string jsonRequest = JsonSerializer.Serialize(activityToSend, _jsonOptions);
            LogRequestData($"Create Activity Request: {jsonRequest}");

            // Sử dụng StringContent để kiểm soát nội dung gửi đi
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            return await SendRequestAsync<DoanKhoaClient.Models.Activity>(
                () => _httpClient.PostAsync("activity", content, cancellationToken),
                "Lỗi khi tạo hoạt động"
            );
        }

        public async Task<DoanKhoaClient.Models.Activity> UpdateActivityAsync(string id, DoanKhoaClient.Models.Activity activity, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("ID không thể trống", nameof(id));
            }

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity), "Activity cannot be null");
            }

            try
            {
                // Tạo một bản sao để tránh thay đổi đối tượng gốc
                var activityToUpdate = new DoanKhoaClient.Models.Activity
                {
                    Id = id, // Sử dụng ID từ tham số
                    Title = activity.Title,
                    Description = activity.Description,
                    Type = activity.Type,
                    Date = activity.Date,
                    ImgUrl = activity.ImgUrl,
                    CreatedAt = activity.CreatedAt,
                    Status = activity.Status
                };

                string jsonRequest = JsonSerializer.Serialize(activityToUpdate, _jsonOptions);
                LogRequestData($"Update Activity Request: {jsonRequest}");

                // Sử dụng StringContent để kiểm soát nội dung gửi đi
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Gửi request và lấy response
                var response = await _httpClient.PutAsync($"activity/{id}", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponseData($"Update Activity Response Status: {response.StatusCode}");
                LogResponseData($"Update Activity Response Content: {responseContent}");

                // Kiểm tra kết quả
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Máy chủ trả về lỗi: {response.StatusCode} - {responseContent}");
                }

                // Xử lý trường hợp phản hồi rỗng
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    // Nếu server trả về rỗng nhưng thành công (204 No Content hoặc 200 OK),
                    // chúng ta có thể trả về activity gốc đã cập nhật
                    LogResponseData("Server trả về dữ liệu rỗng, sử dụng activity đã cập nhật");
                    return activityToUpdate;
                }

                // Phân tích phản hồi JSON (nếu có)
                try
                {
                    var updatedActivity = JsonSerializer.Deserialize<DoanKhoaClient.Models.Activity>(responseContent, _jsonOptions);
                    return updatedActivity;
                }
                catch (JsonException ex)
                {
                    LogResponseData($"Lỗi phân tích JSON: {ex.Message}. Sử dụng activity đã cập nhật.");
                    // Nếu không thể phân tích JSON, trả về activity đã cập nhật
                    return activityToUpdate;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật hoạt động: {ex.Message}", ex);
            }
        }

        public async Task DeleteActivityAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("ID không thể trống", nameof(id));
            }

            try
            {
                // Gửi request xóa
                var response = await _httpClient.DeleteAsync($"activity/{id}", cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponseData($"Delete Activity Response Status: {response.StatusCode}");
                LogResponseData($"Delete Activity Response Content: {responseContent}");

                // Kiểm tra kết quả
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Máy chủ trả về lỗi: {response.StatusCode} - {responseContent}");
                }

                // Nếu thành công, không cần trả về gì
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa hoạt động: {ex.Message}", ex);
            }
        }

        public async Task<bool> ToggleParticipationAsync(string activityId, string userId)
        {
            if (string.IsNullOrEmpty(activityId))
                throw new ArgumentException("ActivityId cannot be null or empty", nameof(activityId));
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

            try
            {
                var response = await _httpClient.PostAsync($"activity/{activityId}/participate?userId={userId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogResponseData($"Error in ToggleParticipationAsync: {ex.Message}");
                throw new Exception($"Lỗi khi cập nhật trạng thái tham gia: {ex.Message}");
            }
        }

        public async Task<bool> ToggleLikeAsync(string activityId, string userId)
        {
            if (string.IsNullOrEmpty(activityId))
                throw new ArgumentException("ActivityId cannot be null or empty", nameof(activityId));
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

            try
            {
                var response = await _httpClient.PostAsync($"activity/{activityId}/like?userId={userId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogResponseData($"Error in ToggleLikeAsync: {ex.Message}");
                throw new Exception($"Lỗi khi cập nhật trạng thái yêu thích: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, bool>> GetUserActivityStatusAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"activity/user-status/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
                }
                throw new Exception("Không thể lấy trạng thái hoạt động của người dùng");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy trạng thái hoạt động: {ex.Message}");
            }
        }

        // Phương thức trợ giúp để tránh lặp code
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
                    throw new HttpRequestException($"Máy chủ trả về lỗi: {response.StatusCode} - {responseContent}");
                }

                // Xử lý trường hợp phản hồi rỗng
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    if (typeof(T) == typeof(object))
                    {
                        return default;
                    }
                    throw new JsonException("Server trả về dữ liệu rỗng");
                }

                // Xử lý trường hợp phản hồi không phải JSON
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
                    // Ghi log nội dung phản hồi để debug
                    Debug.WriteLine($"JSON không hợp lệ: {responseContent}");
                    LogResponseData($"JSON không hợp lệ: {ex.Message}");

                    // Kiểm tra xem có phải HTML không
                    if (responseContent.Contains("<!DOCTYPE html>") || responseContent.Contains("<html"))
                    {
                        throw new Exception("Server trả về HTML thay vì JSON. Có thể server gặp lỗi 500.");
                    }

                    throw new Exception($"Lỗi xử lý dữ liệu từ máy chủ: {ex.Message}. Dữ liệu trả về không phải JSON hợp lệ.");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Không thể kết nối đến máy chủ: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Yêu cầu đã hết thời gian chờ. Vui lòng thử lại.");
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

        // Logger có thể được thay thế bằng hệ thống logging thực tế
        private void LogRequestData(string message)
        {
            Debug.WriteLine($"[Request] {message}");
            Console.WriteLine($"[Request] {message}");
        }

        private void LogResponseData(string message)
        {
            Debug.WriteLine($"[Response] {message}");
            Console.WriteLine($"[Response] {message}");
        }
    }
}