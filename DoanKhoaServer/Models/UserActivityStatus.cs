using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DoanKhoaServer.Models
{
    public class UserActivityStatus
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ActivityId { get; set; }

        public bool IsJoined { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}