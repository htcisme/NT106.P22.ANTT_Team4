using System;
using System.IO;
using System.Text.Json;

namespace DoanKhoaClient.Properties
{
    public class Settings
    {
        private static Settings _default;
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DoanKhoaClient",
            "settings.json"
        );

        public static Settings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = LoadSettings();
                }
                return _default;
            }
        }

        // Các cài đặt lọc spam
        public bool AutoFilterSpam { get; set; } = true;
        public bool NotifyOnSpamDetection { get; set; } = true;
        public int SpamFilterLevel { get; set; } = 5;

        // Thêm các cài đặt người dùng cho tính năng bình luận
        public string CurrentUserId { get; set; } = "676b4e0e2d5a8b1234567890"; // Đặt ID mặc định
        public string CurrentUserDisplayName { get; set; } = "Test User";
        public string CurrentUserAvatar { get; set; } = "";
        public string CurrentUserEmail { get; set; } = "test@example.com";
        public string CurrentUserRole { get; set; } = "user";

        // Các cài đặt khác giữ nguyên...
        public bool IsDarkMode { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public bool EnableCommentNotifications { get; set; } = true;
        public bool EnableActivityNotifications { get; set; } = true;
        public bool EnableSoundNotifications { get; set; } = false;
        public int CommentsPerPage { get; set; } = 20;
        public bool EnableImageCaching { get; set; } = true;
        public bool EnableLazyLoading { get; set; } = true;

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static Settings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);

                    // Đảm bảo có UserId mặc định
                    if (settings != null && string.IsNullOrEmpty(settings.CurrentUserId))
                    {
                        settings.CurrentUserId = "676b4e0e2d5a8b1234567890";
                        settings.CurrentUserDisplayName = "Test User";
                        settings.CurrentUserEmail = "test@example.com";
                        settings.Save();
                    }

                    return settings ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new Settings();
        }

        // Helper methods - giữ nguyên
        public void SetCurrentUser(string userId, string displayName, string avatar = "", string email = "", string role = "")
        {
            CurrentUserId = userId ?? "676b4e0e2d5a8b1234567890";
            CurrentUserDisplayName = displayName ?? "Test User";
            CurrentUserAvatar = avatar ?? "";
            CurrentUserEmail = email ?? "test@example.com";
            CurrentUserRole = role ?? "user";
            Save();
        }

        public void ClearCurrentUser()
        {
            CurrentUserId = "676b4e0e2d5a8b1234567890"; // Không để trống
            CurrentUserDisplayName = "Test User";
            CurrentUserAvatar = "";
            CurrentUserEmail = "test@example.com";
            CurrentUserRole = "user";
            Save();
        }

        public bool HasCurrentUser()
        {
            return !string.IsNullOrEmpty(CurrentUserId) && CurrentUserId != "676b4e0e2d5a8b1234567890";
        }

        public void Reset()
        {
            AutoFilterSpam = true;
            NotifyOnSpamDetection = true;
            SpamFilterLevel = 5;
            IsDarkMode = false;
            Theme = "Light";
            EnableCommentNotifications = true;
            EnableActivityNotifications = true;
            EnableSoundNotifications = false;
            CommentsPerPage = 20;
            EnableImageCaching = true;
            EnableLazyLoading = true;

            // Đặt lại user mặc định thay vì xóa hoàn toàn
            CurrentUserId = "676b4e0e2d5a8b1234567890";
            CurrentUserDisplayName = "Test User";
            CurrentUserAvatar = "";
            CurrentUserEmail = "test@example.com";
            CurrentUserRole = "user";

            Save();
        }
    }
}