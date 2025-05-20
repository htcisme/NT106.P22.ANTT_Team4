using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

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

    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime LastSeen { get; set; }
        public List<string> Conversations { get; set; } = new List<string>();
        public UserRole Role { get; set; } // Default to regular user
                                           // Add these properties to the User clas
        public Position Position { get; set; } = Position.DoanVien;
        public int ActivitiesCount { get; set; } = 0;

        // Existing two-factor auth properties
        public bool TwoFactorEnabled { get; set; }
        public string TwoFactorSecret { get; set; }
        public DateTime? TwoFactorCodeExpiry { get; set; }
        public string CurrentTwoFactorCode { get; set; }

        // Email verification properties
        public bool EmailVerified { get; set; } = false;
        public string EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiry { get; set; }
    }
}