using System;

namespace DoanKhoaClient.Models
{
    // DTO chuyên dùng để tạo mới TaskSession, không có trường ID
    public class CreateTaskSessionDto
    {
        public string Name { get; set; }
        public string ManagerId { get; set; }
        public string ManagerName { get; set; }
        public TaskSessionType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Từ TaskSession tạo ra DTO
        public static CreateTaskSessionDto FromTaskSession(TaskSession session)
        {
            return new CreateTaskSessionDto
            {
                Name = session.Name,
                ManagerId = session.ManagerId,
                ManagerName = session.ManagerName,
                Type = session.Type,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
    }
}