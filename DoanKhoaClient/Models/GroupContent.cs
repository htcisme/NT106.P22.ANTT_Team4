using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace DoanKhoaClient.Models
{
    public class GroupContent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string SessionId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedBy { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties (không lưu trong DB)
        [BsonIgnore]
        public TaskSession Session { get; set; }

        [BsonIgnore]
        public string CreatedByName { get; set; }

        [BsonIgnore]
        public int TaskCount { get; set; }

        [BsonIgnore]
        public int CompletedTaskCount { get; set; }

        public GroupContent()
        {
            Id = ObjectId.GenerateNewId().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}