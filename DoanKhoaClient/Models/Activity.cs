using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Models
{
    public class Activity : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; }
        public string Description { get; set; }
        public ActivityType Type { get; set; }
        public DateTime Date { get; set; }
        public string ImgUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActivityStatus Status { get; set; }

        // Thuộc tính mới để hỗ trợ chọn hàng loạt
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
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

    public static class ActivityStatusEnum
    {
        public static Array Values => Enum.GetValues(typeof(ActivityStatus));
    }
}