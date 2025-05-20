using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoanKhoaServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        public FileController(MongoDBService mongoDBService, IWebHostEnvironment environment)
        {
            _mongoDBService = mongoDBService;
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Attachment>> UploadFile([FromForm] AttachmentUploadModel model)
        {
            if (model.File == null)
                return BadRequest("No file provided");

            try
            {
                // Đảm bảo thư mục Uploads tồn tại
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Tạo tên file duy nhất bằng cách sử dụng timestamp và GUID
                string uniqueFileName = $"{DateTime.Now.Ticks}_{Guid.NewGuid()}_{model.File.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // Xác định nếu file là hình ảnh
                bool isImage = model.File.ContentType.StartsWith("image/");

                // Tạo đối tượng Attachment với thông tin URL cố định
                var attachment = new Attachment
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FileName = model.File.FileName,
                    ContentType = model.File.ContentType,
                    FilePath = filePath,
                    // Lưu đường dẫn tương đối để có thể truy cập qua HTTP
                    FileUrl = $"/api/attachments/download/{uniqueFileName}",
                    FileSize = model.File.Length,
                    IsImage = isImage,
                    UploadDate = DateTime.UtcNow,
                    UploaderId = model.UploaderId
                };

                // Lưu vào database
                await _mongoDBService.SaveAttachmentAsync(attachment);

                return attachment;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                // Tìm đường dẫn đầy đủ của file
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound($"File {fileName} not found");

                // Xác định MIME type
                string contentType = GetContentType(Path.GetExtension(fileName));

                // Đọc file và trả về
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, contentType, Path.GetFileName(fileName));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string GetContentType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".zip": return "application/zip";
                case ".rar": return "application/x-rar-compressed";
                default: return "application/octet-stream";
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Download(string id)
        {
            var attachment = await _mongoDBService.GetAttachmentByIdAsync(id);
            if (attachment == null)
                return NotFound();

            if (!System.IO.File.Exists(attachment.FilePath))
                return NotFound();

            var memory = new MemoryStream();
            using (var stream = new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, attachment.ContentType, attachment.FileName);
        }

        [HttpGet("thumbnail/{id}")]
        public async Task<IActionResult> GetThumbnail(string id)
        {
            var attachment = await _mongoDBService.GetAttachmentByIdAsync(id);
            if (attachment == null || !attachment.IsImage)
                return NotFound();

            var filePath = attachment.ThumbnailPath;
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                filePath = attachment.FilePath; // Nếu không có thumbnail, trả về file gốc

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, attachment.ContentType);
        }
    }
}