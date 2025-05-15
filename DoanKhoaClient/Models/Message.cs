using System;
using System.Collections.Generic;

namespace DoanKhoaClient.Models
{
    public class Message
    {
        public string Id { get; set; }
        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; } // Added property

        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }

    public enum MessageType
    {
        Text,
        Image,
        File,
        System
    }

    public class Attachment
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public bool IsImage => ContentType.StartsWith("image/");
        public string ThumbnailUrl { get; set; }
    }
}