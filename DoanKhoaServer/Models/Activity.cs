using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DoanKhoaServer.Models
{
    public class Activity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ActivityType Type { get; set; }
        public DateTime Date { get; set; }
        public string ImgUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActivityStatus Status { get; set; }
    }

    public enum ActivityType
    {
        Academic,
        Volunteer,
        Entertainment
    }

    public enum ActivityStatus
    {
        Upcoming,
        Ongoing,
        Completed,
    }
    public static class ActivityTypeEnum
    {
        public static Array Values => Enum.GetValues(typeof(ActivityType));
    }
}
