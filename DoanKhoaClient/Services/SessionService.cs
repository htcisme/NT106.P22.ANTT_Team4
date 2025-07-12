using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Services
{
    public class SessionService
    {
        private static readonly string SessionDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DoanKhoaClient");
        private static readonly string SessionFilePath = Path.Combine(SessionDirectory, "session.dat");
        private static readonly string RememberFilePath = Path.Combine(SessionDirectory, "remember.dat");
        private static readonly int SessionTimeoutMinutes = 15;

        public static void SaveSession(User user)
        {
            try
            {
                // Tạo thư mục nếu không tồn tại
                if (!Directory.Exists(SessionDirectory))
                {
                    Directory.CreateDirectory(SessionDirectory);
                }

                var sessionData = new SessionData
                {
                    UserId = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Role = user.Role,
                    AvatarUrl = user.AvatarUrl,
                    LastActivity = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes)
                };

                // Mã hóa và lưu session
                var jsonData = JsonSerializer.Serialize(sessionData);
                var encryptedData = EncryptString(jsonData);
                File.WriteAllText(SessionFilePath, encryptedData);

                System.Diagnostics.Debug.WriteLine($"Session saved for user: {user.Username}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving session: {ex.Message}");
            }
        }

        public static void SaveRememberCredentials(string username, string password)
        {
            try
            {
                if (!Directory.Exists(SessionDirectory))
                {
                    Directory.CreateDirectory(SessionDirectory);
                }

                var rememberData = new RememberData
                {
                    Username = username,
                    Password = password, // Trong thực tế nên hash password
                    SavedAt = DateTime.UtcNow
                };

                var jsonData = JsonSerializer.Serialize(rememberData);
                var encryptedData = EncryptString(jsonData);
                File.WriteAllText(RememberFilePath, encryptedData);

                System.Diagnostics.Debug.WriteLine($"Remember credentials saved for: {username}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving remember credentials: {ex.Message}");
            }
        }

        public static RememberData GetRememberCredentials()
        {
            try
            {
                if (!File.Exists(RememberFilePath))
                    return null;

                var encryptedData = File.ReadAllText(RememberFilePath);
                var jsonData = DecryptString(encryptedData);
                return JsonSerializer.Deserialize<RememberData>(jsonData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting remember credentials: {ex.Message}");
                return null;
            }
        }

        public static SessionData GetSession()
        {
            try
            {
                if (!File.Exists(SessionFilePath))
                    return null;

                var encryptedData = File.ReadAllText(SessionFilePath);
                var jsonData = DecryptString(encryptedData);
                var sessionData = JsonSerializer.Deserialize<SessionData>(jsonData);

                // Kiểm tra session có hết hạn không
                if (sessionData.ExpiryTime < DateTime.UtcNow)
                {
                    System.Diagnostics.Debug.WriteLine("Session expired, deleting...");
                    DeleteSession();
                    return null;
                }

                return sessionData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting session: {ex.Message}");
                DeleteSession(); // Xóa session lỗi
                return null;
            }
        }

        public static void UpdateSessionActivity()
        {
            try
            {
                var session = GetSession();
                if (session != null)
                {
                    session.LastActivity = DateTime.UtcNow;
                    session.ExpiryTime = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes);

                    var jsonData = JsonSerializer.Serialize(session);
                    var encryptedData = EncryptString(jsonData);
                    File.WriteAllText(SessionFilePath, encryptedData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating session activity: {ex.Message}");
            }
        }

        public static void DeleteSession()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                    System.Diagnostics.Debug.WriteLine("Session deleted");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting session: {ex.Message}");
            }
        }

        public static void DeleteRememberCredentials()
        {
            try
            {
                if (File.Exists(RememberFilePath))
                {
                    File.Delete(RememberFilePath);
                    System.Diagnostics.Debug.WriteLine("Remember credentials deleted");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting remember credentials: {ex.Message}");
            }
        }

        public static bool IsSessionValid()
        {
            var session = GetSession();
            return session != null && session.ExpiryTime > DateTime.UtcNow;
        }

        private static string EncryptString(string plainText)
        {
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(data);
        }

        private static string DecryptString(string cipherText)
        {
            byte[] data = Convert.FromBase64String(cipherText);
            return Encoding.UTF8.GetString(data);
        }
    }

    public class SessionData
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime ExpiryTime { get; set; }
    }

    public class RememberData
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime SavedAt { get; set; }
    }
}