using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace DoanKhoaServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentsController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AttachmentsController(MongoDBService mongoDBService, IWebHostEnvironment webHostEnvironment)
        {
            _mongoDBService = mongoDBService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Attachment>> UploadFile([FromForm] AttachmentUploadModel model)
        {
            try
            {
                if (model.File == null || model.File.Length == 0)
                    return BadRequest("No file was uploaded.");

                // Đảm bảo thư mục Uploads tồn tại
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Tạo tên file đơn giản với timestamp để tránh trùng lặp
                string uniqueFileName = $"{Path.GetFileName(model.File.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Lưu file vào ổ đĩa
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // Xác định nếu file là hình ảnh
                bool isImage = model.File.ContentType.StartsWith("image/");

                // QUAN TRỌNG: Lưu URL với định dạng chuẩn /Uploads/filename
                string fileUrl = $"/Uploads/{uniqueFileName}";

                Console.WriteLine($"Uploaded file: {uniqueFileName}, Path: {filePath}, URL: {fileUrl}");

                // Tạo bản ghi attachment
                var attachment = new Attachment
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FileName = model.File.FileName,
                    ContentType = model.File.ContentType,
                    FilePath = filePath,
                    FileUrl = fileUrl,
                    FileSize = model.File.Length,
                    IsImage = isImage,
                    UploadDate = DateTime.UtcNow,
                    UploaderId = model.UploaderId,
                    MessageId = model.MessageId,

                    // Set default thumbnail values
                    ThumbnailPath = isImage ?
        Path.Combine(uploadsFolder, "default_thumbnail.png") :
        Path.Combine(uploadsFolder, "default_file_thumbnail.png"),
                    ThumbnailUrl = isImage ?
        "/Uploads/default_thumbnail.png" :
        "/Uploads/default_file_thumbnail.png"
                };

                // Lưu vào database
                await _mongoDBService.SaveAttachmentAsync(attachment);

                // Trả về đầy đủ URL cho client - chỉ để hiển thị ngay sau khi tải lên
                attachment.FileUrl = $"{Request.Scheme}://{Request.Host}{fileUrl}";

                return attachment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
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

                return File(memory, contentType, fileName);
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
        public async Task<ActionResult<Attachment>> GetAttachment(string id)
        {
            var attachment = await _mongoDBService.GetAttachmentByIdAsync(id);

            if (attachment == null)
                return NotFound();

            // Thêm URL cho client
            attachment.FileUrl = $"{Request.Scheme}://{Request.Host}/api/attachments/download/{attachment.Id}";

            return attachment;
        }
    }

}