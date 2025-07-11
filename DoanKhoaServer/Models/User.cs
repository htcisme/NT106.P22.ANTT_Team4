using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoanKhoaServer.Models
{
    public enum UserRole
    {
        All = -1,
        User = 0,
        Admin = 1
    }
    public enum Position
    {
        DoanVien = 0,
        CongTacVien = 1,
        UyVienBCHMoRong = 2,
        UyVienBCH = 3,
        UyVienBTV = 4
    }

    public class UpdateUserDto
    {
        [Required]
        public string DisplayName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string AvatarUrl { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [Required]
        public Position Position { get; set; }
    }

    public class BatchUpdateUserDto
    {
        public UserRole? Role { get; set; }
        public Position? Position { get; set; }
        public bool? EmailVerified { get; set; }
    }

    public class BatchUpdateUserRequest
    {
        public List<string> UserIds { get; set; } = new List<string>();
        public BatchUpdateUserDto Updates { get; set; }
        public string AdminCode { get; set; } // Cho việc nâng cấp lên Admin
    }


    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string DisplayName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string PasswordSalt { get; set; } = string.Empty;

        public string AvatarUrl { get; set; } = string.Empty; 

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public List<string> Conversations { get; set; } = new List<string>();

        public UserRole Role { get; set; } = UserRole.User; 

        public Position Position { get; set; } = Position.DoanVien;

        public int ActivitiesCount { get; set; } = 0;

        // Two-factor auth properties với default values
        public bool TwoFactorEnabled { get; set; } = false;

        public string TwoFactorSecret { get; set; } = string.Empty;

        public DateTime? TwoFactorCodeExpiry { get; set; }

        public string CurrentTwoFactorCode { get; set; } = string.Empty;

        // Email verification properties với default values
        public bool EmailVerified { get; set; } = false;

        public string EmailVerificationCode { get; set; } = string.Empty;

        public DateTime? EmailVerificationCodeExpiry { get; set; }
    }
}