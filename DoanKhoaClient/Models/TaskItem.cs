using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace DoanKhoaClient.Models
{
    public class TaskItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("assignedToId")]
        public string AssignedToId { get; set; }

        [JsonProperty("assignedToName")]
        public string AssignedToName { get; set; }

        [JsonProperty("programId")]
        public string ProgramId { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonProperty("status")]
        public TaskItemStatus Status { get; set; } = TaskItemStatus.NotStarted;

        [JsonProperty("priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    }

    public enum TaskItemStatus
    {
        [Description("Chưa bắt đầu")]
        NotStarted,

        [Description("Đang thực hiện")]
        InProgress,

        [Description("Hoàn thành")]
        Completed,

        [Description("Hủy")]
        Canceled,

        [Description("Tạm hoãn")]
        Delayed,

        [Description("Chờ xử lý")]
        Pending
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}