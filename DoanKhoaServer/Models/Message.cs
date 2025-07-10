using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace DoanKhoaServer.Models
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public bool IsSpam { get; set; } = false;
        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; } = false; // Thêm thuộc tính này

        public MessageType Type { get; set; } = MessageType.Text;
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
    public class UpdateMessageRequest
    {
        public string Content { get; set; }
        public string UserId { get; set; }
    }
    public enum MessageType
    {
        Text,
        Image,
        File,
        System
    }
}