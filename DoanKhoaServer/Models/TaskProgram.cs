using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DoanKhoaServer.Models
{
    public class TaskProgram
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string SessionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ProgramType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum ProgramType
    {
        Event,
        Study,
        Design
    }
}