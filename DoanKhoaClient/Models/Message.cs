using System;
using System.Collections.Generic;
using System.ComponentModel; // Thêm namespace này
using System.Runtime.CompilerServices; // Thêm namespace này

namespace DoanKhoaClient.Models
{
    public class Message : INotifyPropertyChanged
    {
        private string _content;
        private bool _isEdited;
        public string Id { get; set; }
        public bool IsSpam { get; set; } = false;
        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; } // Added property

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                if (_isEdited != value)
                {
                    _isEdited = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum MessageType
    {
        Text,
        Image,
        File,
        System
    }
    public class Attachment : INotifyPropertyChanged
    {
        private string _fileUrl;
        private bool _isImage;
        private string _contentType;

        public string Id { get; set; }
        public string MessageId { get; set; }
        public string FileName { get; set; }

        public string ContentType
        {
            get => _contentType;
            set
            {
                if (_contentType != value)
                {
                    _contentType = value;
                    // Cập nhật IsImage khi ContentType thay đổi
                    _isImage = _contentType?.StartsWith("image/") ?? false;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsImage));
                }
            }
        }

        public string FilePath { get; set; }

        public string FileUrl
        {
            get => _fileUrl;
            set
            {
                if (_fileUrl != value)
                {
                    _fileUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public long FileSize { get; set; }

        // Chuyển thành property với getter/setter thay vì expression-bodied property
        public bool IsImage
        {
            get => _isImage;
            set
            {
                if (_isImage != value)
                {
                    _isImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime UploadDate { get; set; }
        public string UploaderId { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ThumbnailPath { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}