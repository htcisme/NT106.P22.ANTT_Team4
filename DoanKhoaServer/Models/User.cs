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

        // Existing two-factor auth properties
        public bool TwoFactorEnabled { get; set; }
        public string TwoFactorSecret { get; set; }
        public DateTime? TwoFactorCodeExpiry { get; set; }
        public string CurrentTwoFactorCode { get; set; }
    }
}