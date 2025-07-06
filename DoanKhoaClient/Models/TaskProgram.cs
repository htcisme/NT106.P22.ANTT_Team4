using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;        // ✅ THÊM: For ObservableCollection
using System.ComponentModel;                 // ✅ THÊM: For INotifyPropertyChanged
using System.Runtime.CompilerServices;       // ✅ THÊM: For CallerMemberName

namespace DoanKhoaClient.Models
{
    public class TaskProgram : INotifyPropertyChanged
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime EndDate { get; set; }

        private ObservableCollection<TaskItem> _taskItems;

        // ✅ ADD: Collection of TaskItems
        [JsonProperty("taskItems")]
        public ObservableCollection<TaskItem> TaskItems
        {
            get => _taskItems ??= new ObservableCollection<TaskItem>();
            set => SetProperty(ref _taskItems, value);
        }

        // ✅ ADD: Helper property
        [JsonIgnore]
        public int TaskItemsCount => TaskItems?.Count ?? 0;

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        // Thêm các trường cho người thực hiện
        [JsonProperty("executorId")]
        public string ExecutorId { get; set; }

        [JsonProperty("executorName")]
        public string ExecutorName { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("type")]
        public ProgramType Type { get; set; } = ProgramType.Event; // Mặc định là Event

        [JsonProperty("status")]
        public ProgramStatus Status { get; set; } = ProgramStatus.NotStarted;

        // ✅ ADD: INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // ✅ ADD: Helper methods
        public void AddTaskItem(TaskItem taskItem)
        {
            if (taskItem != null && !TaskItems.Contains(taskItem))
            {
                TaskItems.Add(taskItem);
                taskItem.ProgramId = Id; // Ensure relationship
                OnPropertyChanged(nameof(TaskItemsCount));
            }
        }

        public void RemoveTaskItem(TaskItem taskItem)
        {
            if (TaskItems.Remove(taskItem))
            {
                OnPropertyChanged(nameof(TaskItemsCount));
            }
        }

        public void ClearTaskItems()
        {
            TaskItems.Clear();
            OnPropertyChanged(nameof(TaskItemsCount));
        }
    }

    public enum ProgramType
    {
        Study = 0,    // 0 - Học tập (Training Cuối kỳ có Type = 0)
        Design = 1,   // 1 - Thiết kế
        Event = 2     // 2 - Sự kiện
    }

    public enum ProgramStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Canceled
    }
}