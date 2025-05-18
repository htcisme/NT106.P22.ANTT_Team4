using System;

namespace DoanKhoaClient.Models
{
    // Corrected version of UserActivityStatus.cs
    public class UserActivityStatus
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ActivityId { get; set; }
        public bool IsJoined { get; set; }
        public bool IsFavorite { get; set; }

        // Constructor
        public UserActivityStatus()
        {
            Id = Guid.NewGuid().ToString();
        }

        // Constructor with parameters
        public UserActivityStatus(string userId, string activityId, bool isJoined = false, bool isFavorite = false)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            ActivityId = activityId;
            IsJoined = isJoined;
            IsFavorite = isFavorite;
        }
    }
}