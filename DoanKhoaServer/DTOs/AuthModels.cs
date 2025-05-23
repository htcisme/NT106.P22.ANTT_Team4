﻿using DoanKhoaServer.Models;

namespace DoanKhoaServer.DTOs
{
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
        public string Message { get; set; } = ""; // Default initialization to avoid null
        public bool RequiresEmailVerification { get; set; }
    }
}