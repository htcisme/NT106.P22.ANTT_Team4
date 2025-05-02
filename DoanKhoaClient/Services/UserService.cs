using DoanKhoaClient.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DoanKhoaClient.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;

        public UserService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };
        }

        public async Task<User> GetCurrentUserAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("user/current");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>();
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

        public async Task<System.Collections.Generic.List<User>> SearchUsersAsync(string query)
        {
            try
            {
                var response = await _httpClient.GetAsync($"user/search?query={Uri.EscapeDataString(query)}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<User>>();
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
    }
}