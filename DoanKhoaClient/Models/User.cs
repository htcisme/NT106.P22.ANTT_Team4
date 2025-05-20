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

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool EnableTwoFactorAuth { get; set; } = false;
        public UserRole Role { get; set; } = UserRole.User;
        public string AdminCode { get; set; } // Add this property
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

    public class User : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime LastSeen { get; set; }
        public List<string> Conversations { get; set; } = new List<string>();
        public bool TwoFactorEnabled { get; set; }
        public UserRole Role { get; set; } = UserRole.User; // Default to regular user
        public Position Position { get; set; } = Position.None;
        public bool EmailVerified { get; set; } = false;
        public string EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiry { get; set; }
        public int ActivitiesCount { get; set; } = 0;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}