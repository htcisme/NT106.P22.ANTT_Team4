using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Models
{
    public class Comment : INotifyPropertyChanged
    {
        private string _id;
        private string _activityId;
        private string _userId;
        private string _userDisplayName;
        private string _userAvatar;
        private string _content;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private string _parentCommentId;
        private int _likeCount;
        private bool _isLiked;
        private bool _isOwner;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string ActivityId
        {
            get => _activityId;
            set { _activityId = value; OnPropertyChanged(); }
        }

        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public string UserDisplayName
        {
            get => _userDisplayName;
            set { _userDisplayName = value; OnPropertyChanged(); }
        }

        public string UserAvatar
        {
            get => _userAvatar;
            set { _userAvatar = value; OnPropertyChanged(); }
        }

        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeAgo)); }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(); }
        }

        public string ParentCommentId
        {
            get => _parentCommentId;
            set { _parentCommentId = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsReply)); }
        }

        public int LikeCount
        {
            get => _likeCount;
            set { _likeCount = value; OnPropertyChanged(); }
        }

        public bool IsLiked
        {
            get => _isLiked;
            set { _isLiked = value; OnPropertyChanged(); }
        }

        public bool IsOwner
        {
            get => _isOwner;
            set { _isOwner = value; OnPropertyChanged(); }
        }

        // FIX: Improved TimeAgo calculation using UTC time
        public string TimeAgo
        {
            get
            {
                try
                {
                    // Ensure we're comparing UTC times
                    DateTime utcNow = DateTime.UtcNow;
                    DateTime commentTime = CreatedAt.Kind == DateTimeKind.Utc ? CreatedAt : CreatedAt.ToUniversalTime();

                    var timespan = utcNow - commentTime;

                    // Handle negative timespan (future dates)
                    if (timespan.TotalSeconds < 0)
                    {
                        return "Vừa xong";
                    }

                    if (timespan.TotalSeconds < 30)
                        return "Vừa xong";
                    if (timespan.TotalMinutes < 1)
                        return $"{(int)timespan.TotalSeconds} giây trước";
                    if (timespan.TotalMinutes < 60)
                        return $"{(int)timespan.TotalMinutes} phút trước";
                    if (timespan.TotalHours < 24)
                        return $"{(int)timespan.TotalHours} giờ trước";
                    if (timespan.TotalDays < 7)
                        return $"{(int)timespan.TotalDays} ngày trước";
                    if (timespan.TotalDays < 30)
                        return $"{(int)(timespan.TotalDays / 7)} tuần trước";
                    if (timespan.TotalDays < 365)
                        return $"{(int)(timespan.TotalDays / 30)} tháng trước";

                    return commentTime.ToString("dd/MM/yyyy");
                }
                catch (Exception)
                {
                    // Fallback if there's any error in calculation
                    return CreatedAt.ToString("dd/MM/yyyy HH:mm");
                }
            }
        }

        public bool IsReply => !string.IsNullOrEmpty(ParentCommentId);

        // NEW: Helper property for UI styling
        public string CommentType => IsReply ? "Reply" : "Comment";

        // NEW: Indentation for replies
        public int IndentLevel => IsReply ? 1 : 0;

        // NEW: Display format for time with more detail
        public string DetailedTime => CreatedAt.ToString("dd/MM/yyyy lúc HH:mm");

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Rest of the classes remain the same...
    public class CreateCommentRequest
    {
        public string ActivityId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public string? ParentCommentId { get; set; } = null;

        public CreateCommentRequest()
        {
            ParentCommentId = null;
        }

        public void SetParentCommentId(string parentId)
        {
            if (!string.IsNullOrWhiteSpace(parentId) &&
                MongoDB.Bson.ObjectId.TryParse(parentId.Trim(), out _))
            {
                ParentCommentId = parentId.Trim();
            }
            else
            {
                ParentCommentId = null;
            }
        }
    }

    public class UpdateCommentRequest
    {
        public string Content { get; set; }
    }

    public class CommentResponse
    {
        public string Id { get; set; }
        public string ActivityId { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string UserAvatar { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? ParentCommentId { get; set; }
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsOwner { get; set; }
    }
}