using System;
using System.Collections.Generic;

namespace DoanKhoaClient.Models
{
    public class Conversation
    {
        public string Id { get; set; }
        public List<string> ParticipantIds { get; set; } = new List<string>();
        public List<User> Participants { get; set; } = new List<User>();
        public string LastMessageId { get; set; }
        public DateTime LastActivity { get; set; }
        public string Title { get; set; }
        public bool IsGroup { get; set; }
        public string GroupAvatarUrl { get; set; }
        public string LastMessagePreview { get; set; }
        public string CreatorId { get; set; }
        public List<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public int UnreadCount { get; set; }

        // Thêm các thuộc tính còn thiếu
        public Dictionary<string, string> ParticipantNames { get; set; } = new Dictionary<string, string>();
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageTimestamp { get; set; }
        public bool IsAdminOnly { get; set; }

    }

    public class GroupMember
    {
        public string UserId { get; set; }
        public GroupRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public enum GroupRole
    {
        Owner,
        Admin,
        Member
    }
}