using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Models
{
    public class Comment : INotifyPropertyChanged
    {
        private bool _isLiked;
        private int _likeCount;
        private bool _isOwner;
        private string _parentCommentId;
        public string Id { get; set; }
        public string ActivityId { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string UserAvatar { get; set; } = "/Views/Images/User-icon.png";
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


        public string ParentCommentId
        {
            get => _parentCommentId;
            set
            {
                _parentCommentId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReply));
                OnPropertyChanged(nameof(IndentLevel));

                // Debug log
                System.Diagnostics.Debug.WriteLine($"Comment {Id}: ParentCommentId set to {value ?? "null"}, IsReply: {IsReply}");
            }
        }

        // Thông tin về comment được phản hồi
        public string ReplyToUserName { get; set; }
        public string ReplyToUserId { get; set; }
        public string ReplyToContent { get; set; }

        public int LikeCount
        {
            get => _likeCount;
            set
            {
                _likeCount = value;
                OnPropertyChanged();
            }
        }

        public bool IsLiked
        {
            get => _isLiked;
            set
            {
                _isLiked = value;
                OnPropertyChanged();
            }
        }

        public bool IsOwner
        {
            get => _isOwner;
            set
            {
                _isOwner = value;
                OnPropertyChanged();
            }
        }

        // Computed properties
        public bool IsReply
        {
            get
            {
                var result = !string.IsNullOrEmpty(ParentCommentId);
                System.Diagnostics.Debug.WriteLine($"Comment {Id}: IsReply calculated as {result} (ParentCommentId: {ParentCommentId ?? "null"})");
                return result;
            }
        }

        // Cập nhật IndentLevel để tất cả replies đều có cùng mức indent
        public int IndentLevel
        {
            get
            {
                // Tất cả replies (bao gồm reply của reply) đều có cùng mức indent = 2
                var level = IsReply ? 2 : 0;
                System.Diagnostics.Debug.WriteLine($"Comment {Id}: IndentLevel = {level}");
                return level;
            }
        }

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;

                if (timeSpan.TotalMinutes < 1)
                    return "Vừa xong";
                else if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} phút trước";
                else if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} giờ trước";
                else if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} ngày trước";
                else
                    return CreatedAt.ToString("dd/MM/yyyy");
            }
        }

        public string DetailedTime => CreatedAt.ToString("HH:mm dd/MM/yyyy");

        // Display text cho phản hồi
        public string ReplyIndicatorText
        {
            get
            {
                if (!IsReply || string.IsNullOrEmpty(ReplyToUserName))
                    return "";

                return $"Phản hồi {ReplyToUserName}";
            }
        }

        // Preview của comment được phản hồi (nếu có)
        public string ReplyToPreview
        {
            get
            {
                if (string.IsNullOrEmpty(ReplyToContent))
                    return "";

                if (ReplyToContent.Length > 50)
                    return ReplyToContent.Substring(0, 50) + "...";

                return ReplyToContent;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
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
        public DateTime? UpdatedAt { get; set; }
        public string ParentCommentId { get; set; }
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsOwner { get; set; }

        // Thông tin về comment được phản hồi
        public string ReplyToUserName { get; set; }
        public string ReplyToUserId { get; set; }
        public string ReplyToContent { get; set; }
    }

    public class CreateCommentRequest
    {
        public string ActivityId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public string ParentCommentId { get; set; }
    }

    public class UpdateCommentRequest
    {
        public string Content { get; set; }
    }
}