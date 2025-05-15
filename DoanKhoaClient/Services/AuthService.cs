using DoanKhoaClient.Models;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DoanKhoaClient.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5299/api/")
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Đang gửi yêu cầu đăng ký: {System.Text.Json.JsonSerializer.Serialize(request)}");

                var response = await _httpClient.PostAsJsonAsync("user/register", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Phản hồi từ server: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Nội dung phản hồi: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(
                            responseContent,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (result == null)
                        {
                            return new AuthResponse { Message = "Không thể đọc phản hồi từ server." };
                        }

                        return result;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi đọc JSON: {ex.Message}");
                        return new AuthResponse { Message = $"Lỗi xử lý phản hồi: {ex.Message}" };
                    }
                }
                else
                {
                    // Parse error message to get specific error type
                    return new AuthResponse { Message = responseContent };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi request: {ex.Message}");
                return new AuthResponse { Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }
        public async Task<AuthResponse> VerifyEmailAsync(string userId, string code)
        {
            try
            {
                var request = new
                {
                    UserId = userId,
                    Code = code
                };

                var response = await _httpClient.PostAsJsonAsync("user/verify-email", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(
                        responseContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result;
                }
                else
                {
                    return new AuthResponse { Message = responseContent };
                }
            }
            catch (Exception ex)
            {
                return new AuthResponse { Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                Debug.WriteLine($"Đang gửi yêu cầu đăng nhập: {JsonSerializer.Serialize(request)}");

                var response = await _httpClient.PostAsJsonAsync("user/login", request);
                Debug.WriteLine($"Phản hồi từ server: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    Debug.WriteLine($"Đăng nhập thành công: {JsonSerializer.Serialize(result)}");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Lỗi đăng nhập: {errorContent}");

                    // Trả về response với thông báo lỗi
                    return new AuthResponse
                    {
                        Message = $"Đăng nhập thất bại ({response.StatusCode}): {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception khi đăng nhập: {ex}");
                return new AuthResponse
                {
                    Message = $"Lỗi kết nối: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request)
        {
            try
            {
                Debug.WriteLine($"Đang gửi yêu cầu xác thực OTP: {JsonSerializer.Serialize(request)}");

                var response = await _httpClient.PostAsJsonAsync("user/verify-otp", request);
                Debug.WriteLine($"Phản hồi từ server: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    Debug.WriteLine($"Xác thực OTP thành công: {JsonSerializer.Serialize(result)}");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Lỗi xác thực OTP: {errorContent}");

                    return new AuthResponse
                    {
                        Message = $"Xác thực OTP thất bại ({response.StatusCode}): {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception khi xác thực OTP: {ex}");
                return new AuthResponse
                {
                    Message = $"Lỗi kết nối: {ex.Message}"
                };
            }
        }
    }
}