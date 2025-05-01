using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DoanKhoaServer.Models
{
    public class Attachment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public bool IsImage { get; set; }
        public string MessageId { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public string UploaderId { get; set; }

        // Add these properties
        public string ThumbnailPath { get; set; }
        [BsonIgnore]
        public string FileUrl { get; set; }
        [BsonIgnore]
        public string ThumbnailUrl { get; set; }
    }
}