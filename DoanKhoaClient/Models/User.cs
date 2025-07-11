using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Models
{
    public enum UserRole
    {
        All = -1,
        User = 0,
        Admin = 1
    }

    public enum Position
    {
        None = -1,
        DoanVien = 0,
        CongTacVien = 1,
        UyVienBCHMoRong = 2,
        UyVienBCH = 3,
        UyVienBTV = 4
    }

    public class User : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public List<string> Conversations { get; set; } = new List<string>();
        public bool TwoFactorEnabled { get; set; } = false;
        public UserRole Role { get; set; } = UserRole.User;
        public Position Position { get; set; } = Position.None;
        public bool EmailVerified { get; set; } = false;
        public string EmailVerificationCode { get; set; } = string.Empty;
        public DateTime? EmailVerificationCodeExpiry { get; set; }
        public int ActivitiesCount { get; set; } = 0;

        // THÊM CÁC TRƯỜNG BỊ THIẾU
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public string TwoFactorSecret { get; set; } = string.Empty;
        public DateTime? TwoFactorCodeExpiry { get; set; }
        public string CurrentTwoFactorCode { get; set; } = string.Empty;

        // SỬA LẠI PROPERTY IsSelected ĐỂ ĐẢMBẢO BINDING HOẠT ĐỘNG ĐÚNG
        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    // Debug để kiểm tra
                    System.Diagnostics.Debug.WriteLine($"User {DisplayName ?? Username} IsSelected changed to: {value}");
                }
            }
        }

        // Computed property để kiểm tra online status
        public bool IsOnline => LastSeen > DateTime.UtcNow.AddMinutes(-15);

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Các class khác giữ nguyên
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool EnableTwoFactorAuth { get; set; } = false;
        public UserRole Role { get; set; } = UserRole.User;
        public string AdminCode { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string UserId { get; set; }
        public string Otp { get; set; }
    }

    public class AuthResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public UserRole Role { get; set; }
        public string Message { get; set; }
        public bool RequiresEmailVerification { get; set; }
    }
}