using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoanKhoaServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public MessagesController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        // Thêm phương thức này hoặc cập nhật phương thức GetConversationMessages hiện có
        [HttpGet("conversation/{conversationId}")]
        public async Task<ActionResult<List<Message>>> GetConversationMessages(string conversationId)
        {
            try
            {
                var messages = await _mongoDBService.GetMessagesByConversationIdAsync(conversationId);
                Console.WriteLine($"Found {messages.Count} messages for conversation {conversationId}");

                // Xử lý attachments
                foreach (var message in messages)
                {
                    if (message.Attachments != null && message.Attachments.Count > 0)
                    {
                        Console.WriteLine($"Message {message.Id} has {message.Attachments.Count} attachments");

                        foreach (var attachment in message.Attachments)
                        {
                            // Đảm bảo MessageId được thiết lập
                            if (string.IsNullOrEmpty(attachment.MessageId))
                            {
                                attachment.MessageId = message.Id;
                            }

                            // Xử lý FileUrl - ĐƠN GIẢN HÓA LOGIC
                            if (!string.IsNullOrEmpty(attachment.FileUrl))
                            {
                                // Nếu là URL tương đối /Uploads/... thì chuyển thành URL tuyệt đối
                                if (attachment.FileUrl.StartsWith("/Uploads/"))
                                {
                                    string baseUrl = $"{Request.Scheme}://{Request.Host}";
                                    attachment.FileUrl = $"{baseUrl}{attachment.FileUrl}";
                                    Console.WriteLine($"Processed URL: {attachment.FileUrl}");
                                }
                                // Nếu là tên file thuần túy, thêm prefix /Uploads/
                                else if (!attachment.FileUrl.StartsWith("http"))
                                {
                                    string fileName = Path.GetFileName(attachment.FileUrl);
                                    string baseUrl = $"{Request.Scheme}://{Request.Host}";
                                    attachment.FileUrl = $"{baseUrl}/Uploads/{fileName}";
                                    Console.WriteLine($"Converted to URL: {attachment.FileUrl}");
                                }
                                // Các URL khác giữ nguyên (http://, https://)
                            }
                        }
                    }
                }

                return messages.OrderBy(m => m.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetConversationMessages: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // Add this method to MessagesController.cs
        [HttpPost]
        public async Task<ActionResult<Message>> CreateMessage([FromBody] Message message)
        {
            try
            {
                // Validate basic message fields
                if (string.IsNullOrEmpty(message.ConversationId) || string.IsNullOrEmpty(message.SenderId))
                {
                    return BadRequest("ConversationId and SenderId are required");
                }

                // Fix any attachments with missing thumbnail information
                if (message.Attachments != null)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        if (string.IsNullOrEmpty(attachment.ThumbnailPath))
                        {
                            // Set default values
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                            attachment.ThumbnailPath = attachment.IsImage ?
                                Path.Combine(uploadsFolder, "default_thumbnail.png") :
                                Path.Combine(uploadsFolder, "default_file_thumbnail.png");

                            attachment.ThumbnailUrl = attachment.IsImage ?
                                "/Uploads/default_thumbnail.png" :
                                "/Uploads/default_file_thumbnail.png";
                        }
                    }
                }

                // Save the message
                var createdMessage = await _mongoDBService.CreateMessageAsync(message);

                return Ok(createdMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating message: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // Thêm vào MessagesController.cs
        [HttpPost("{id}/markSpam")]
        public async Task<IActionResult> MarkAsSpam(string id)
        {
            try
            {
                var message = await _mongoDBService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound("Message not found");
                }

                // Đánh dấu là spam (thêm thuộc tính hoặc chuyển vào collection riêng)
                bool success = await _mongoDBService.MarkMessageAsSpamAsync(id);

                if (success)
                {
                    return Ok(true);
                }
                else
                {
                    return BadRequest("Không thể đánh dấu tin nhắn");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpPost("{id}/unmarkSpam")]
        public async Task<IActionResult> UnmarkSpam(string id)
        {
            try
            {
                var message = await _mongoDBService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound("Message not found");
                }

                // Bỏ đánh dấu spam
                bool success = await _mongoDBService.UnmarkMessageAsSpamAsync(id);

                if (success)
                {
                    return Ok(true);
                }
                else
                {
                    return BadRequest("Không thể bỏ đánh dấu tin nhắn");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpGet("spam")]
        public async Task<IActionResult> GetSpamMessages()
        {
            try
            {
                var messages = await _mongoDBService.GetSpamMessagesAsync();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreMessage(string id)
        {
            try
            {
                var message = await _mongoDBService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound("Message not found");
                }

                // Khôi phục tin nhắn
                bool success = await _mongoDBService.RestoreMessageAsync(id);

                if (success)
                {
                    return Ok(true);
                }
                else
                {
                    return BadRequest("Không thể khôi phục tin nhắn");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessageById(string id)
        {
            try
            {
                var message = await _mongoDBService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound("Message not found");
                }
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpGet("checkfile")]
        public IActionResult CheckFile(string path)
        {
            try
            {
                // Đường dẫn tuyệt đối đến thư mục Uploads
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

                // Lấy tên file từ đường dẫn đầy đủ
                string fileName = Path.GetFileName(path);

                // Đường dẫn đầy đủ đến file
                string filePath = Path.Combine(uploadsFolder, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    return Ok(new { exists = true, path = filePath });
                }
                else
                {
                    // Kiểm tra tất cả các file trong thư mục Uploads
                    var allFiles = Directory.GetFiles(uploadsFolder)
                        .Select(f => Path.GetFileName(f))
                        .ToList();

                    return NotFound(new { exists = false, path = filePath, allFiles });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error checking file: {ex.Message}");
            }
        }

        // Thêm API endpoint này vào MessagesController
        [HttpGet("listfiles")]
        public IActionResult ListFiles()
        {
            try
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    return NotFound(new { error = "Uploads directory not found" });
                }

                var files = Directory.GetFiles(uploadsFolder)
                    .Select(f => new
                    {
                        fullPath = f,
                        fileName = Path.GetFileName(f),
                        size = new FileInfo(f).Length,
                        created = new FileInfo(f).CreationTime
                    })
                    .ToList();

                return Ok(new { count = files.Count, files });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}