using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DoanKhoaServer.Models
{
    public class TaskItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string ProgramId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AssignedToId { get; set; }
        public string AssignedToName { get; set; }
        public DateTime DueDate { get; set; }
        public TaskItemStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum TaskItemStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}