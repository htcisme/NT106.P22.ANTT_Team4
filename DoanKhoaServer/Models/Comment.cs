using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DoanKhoaServer.Models
{
    public class Comment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("activityId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ActivityId { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // FIX: Proper handling of ParentCommentId to avoid validation errors
        [BsonElement("parentCommentId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfNull] // Important: Ignore if null
        [BsonIgnoreIfDefault] // Also ignore if default value
        public string? ParentCommentId { get; set; } // Make nullable

        [BsonElement("likeCount")]
        [BsonDefaultValue(0)]
        public int LikeCount { get; set; } = 0;

        // Constructor
        public Comment()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ParentCommentId = null; // Explicitly set to null
        }
    }

    // Model for user comment status (like/unlike)
    public class UserCommentStatus
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("commentId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CommentId { get; set; }

        [BsonElement("isLiked")]
        public bool IsLiked { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        public UserCommentStatus()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}