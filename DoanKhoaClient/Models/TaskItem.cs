using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Models
{
    // Đổi tên từ TaskStatus thành TaskItemStatus để tránh xung đột
    public enum TaskItemStatus
    {
        Pending,
        InProgress,
        Completed,
        Canceled
    }

    public class TaskItem : INotifyPropertyChanged
    {
        private string _id;
        private string _programId;
        private string _title;
        private string _description;
        private TaskItemStatus _status; // Đã thay đổi kiểu từ TaskStatus sang TaskItemStatus
        private DateTime _dueDate;
        private DateTime? _completedAt;
        private string _assignedToId;
        private string _assignedToName;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string ProgramId
        {
            get => _programId;
            set { _programId = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public TaskItemStatus Status // Đã thay đổi kiểu từ TaskStatus sang TaskItemStatus
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(); }
        }

        public DateTime? CompletedAt
        {
            get => _completedAt;
            set { _completedAt = value; OnPropertyChanged(); }
        }

        public string AssignedToId
        {
            get => _assignedToId;
            set { _assignedToId = value; OnPropertyChanged(); }
        }

        public string AssignedToName
        {
            get => _assignedToName;
            set { _assignedToName = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}