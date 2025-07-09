using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string _id;
        private string _programId;
        private string _title;
        private string _description;
        private TaskItemStatus _status;
        private TaskPriority _priority;
        private DateTime? _dueDate;
        private DateTime? _completedAt;
        private string _assignedToId;
        private string _assignedToName;
        private string _assignedToEmail; // THÊM field này
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [JsonProperty("programId")]
        public string ProgramId
        {
            get => _programId;
            set => SetProperty(ref _programId, value);
        }

        [JsonProperty("title")]
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [JsonProperty("description")]
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        [JsonProperty("status")]
        public TaskItemStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        [JsonProperty("priority")]
        public TaskPriority Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        [JsonProperty("dueDate")]
        public DateTime? DueDate
        {
            get => _dueDate;
            set => SetProperty(ref _dueDate, value);
        }

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt
        {
            get => _completedAt;
            set => SetProperty(ref _completedAt, value);
        }

        [JsonProperty("assignedToId")]
        public string AssignedToId
        {
            get => _assignedToId;
            set => SetProperty(ref _assignedToId, value);
        }

        [JsonProperty("assignedToName")]
        public string AssignedToName
        {
            get => _assignedToName;
            set => SetProperty(ref _assignedToName, value);
        }

        // THÊM: AssignedToEmail property
        [JsonProperty("assignedToEmail")]
        public string AssignedToEmail
        {
            get => _assignedToEmail;
            set => SetProperty(ref _assignedToEmail, value);
        }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        // THÊM: Compatibility properties cho email reminder
        [JsonIgnore]
        public string AssigneeEmail => AssignedToEmail; // Alias cho compatibility

        [JsonIgnore]
        public string AssigneeName => AssignedToName; // Alias cho compatibility

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // THÊM: Clone method để copy TaskItem
        public TaskItem Clone()
        {
            return new TaskItem
            {
                Id = this.Id,
                ProgramId = this.ProgramId,
                Title = this.Title,
                Description = this.Description,
                Status = this.Status,
                Priority = this.Priority,
                DueDate = this.DueDate,
                CompletedAt = this.CompletedAt,
                AssignedToId = this.AssignedToId,
                AssignedToName = this.AssignedToName,
                AssignedToEmail = this.AssignedToEmail,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt
            };
        }
    }

    public enum TaskItemStatus
    {
        [JsonProperty("notStarted")]
        NotStarted = 0,

        [JsonProperty("inProgress")]
        InProgress = 1,

        [JsonProperty("completed")]
        Completed = 2,

        [JsonProperty("canceled")]
        Canceled = 3,

        [JsonProperty("delayed")]
        Delayed = 4,

        [JsonProperty("pending")]
        Pending = 5
    }

    public enum TaskPriority
    {
        [JsonProperty("low")]
        Low = 0,

        [JsonProperty("medium")]
        Medium = 1,

        [JsonProperty("high")]
        High = 2,

        [JsonProperty("critical")]
        Critical = 3
    }
}