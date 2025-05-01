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
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                // Tạo thư mục nếu chưa tồn tại
                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Tạo tên file duy nhất
                var fileName = Path.GetFileName(file.FileName);
                var fileId = ObjectId.GenerateNewId().ToString();
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                var storedFileName = $"{fileId}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);

                // Lưu file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Kiểm tra xem có phải file hình ảnh không
                var isImage = _allowedImageExtensions.Contains(fileExtension);
                var thumbnailPath = string.Empty;

                // Nếu là hình ảnh, tạo thumbnail
                if (isImage)
                {
                    var thumbnailsFolder = Path.Combine(_environment.ContentRootPath, "Thumbnails");
                    if (!Directory.Exists(thumbnailsFolder))
                        Directory.CreateDirectory(thumbnailsFolder);

                    thumbnailPath = Path.Combine(thumbnailsFolder, storedFileName);
                    // TODO: Implement thumbnail generation logic
                    // Ở đây có thể sử dụng thư viện như ImageSharp để tạo thumbnail
                }

                // Tạo đối tượng Attachment và lưu vào cơ sở dữ liệu
                var attachment = new Attachment
                {
                    Id = fileId,
                    FileName = fileName,
                    FilePath = filePath,
                    FileUrl = $"/api/file/{fileId}",
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    ThumbnailPath = thumbnailPath,
                    IsImage = isImage,
                    UploadDate = DateTime.UtcNow
                };

                // Lưu thông tin file vào database
                await _mongoDBService.SaveAttachmentAsync(attachment);

                return Ok(new
                {
                    Id = fileId,
                    FileName = fileName,
                    FileUrl = $"/api/file/{fileId}",
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    IsImage = isImage,
                    ThumbnailUrl = isImage ? $"/api/file/thumbnail/{fileId}" : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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