using Newtonsoft.Json;
using System;

namespace DoanKhoaClient.Models
{
    public class TaskProgram
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