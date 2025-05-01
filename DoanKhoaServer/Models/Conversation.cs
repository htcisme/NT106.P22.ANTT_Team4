using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace DoanKhoaServer.Models
{
    public class Conversation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Title { get; set; }
        public List<string> ParticipantIds { get; set; } = new List<string>();
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public string LastMessageId { get; set; }

        // Group chat properties
        public bool IsGroup { get; set; }
        public string CreatorId { get; set; }
        public List<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    }

    public class GroupMember
    {
        public string UserId { get; set; }
        public GroupRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}