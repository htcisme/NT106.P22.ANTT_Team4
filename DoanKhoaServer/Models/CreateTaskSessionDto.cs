using System;

namespace DoanKhoaServer.Models
{
    public class CreateTaskSessionDto
    {
        public string Name { get; set; }
        public string ManagerId { get; set; }
        public string ManagerName { get; set; }
        public TaskSessionType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}