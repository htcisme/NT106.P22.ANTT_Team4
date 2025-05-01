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
            if (model.File == null)
                return BadRequest("No file provided");

            try
            {
                // Tạo thư mục lưu trữ nếu chưa tồn tại
                string uploadsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Tạo tên file duy nhất
                string uniqueFileName = $"{DateTime.Now.Ticks}_{Guid.NewGuid()}_{model.File.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Lưu file vào hệ thống file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // Kiểm tra file có phải là hình ảnh không
                bool isImage = model.File.ContentType.StartsWith("image/");

                // Tạo bản ghi attachment
                var attachment = new Attachment
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FileName = model.File.FileName,
                    ContentType = model.File.ContentType,
                    FilePath = filePath,
                    FileSize = model.File.Length,
                    IsImage = isImage,
                    UploadDate = DateTime.UtcNow,
                    UploaderId = model.UploaderId
                };

                // Lưu vào database
                await _mongoDBService.SaveAttachmentAsync(attachment);

                // Tạo URL để client có thể tải file
                attachment.FileUrl = $"{Request.Scheme}://{Request.Host}/api/attachments/download/{attachment.Id}";

                return attachment;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(string id)
        {
            var attachment = await _mongoDBService.GetAttachmentByIdAsync(id);

            if (attachment == null)
                return NotFound();

            if (!System.IO.File.Exists(attachment.FilePath))
                return NotFound("File not found on server");

            var fileBytes = System.IO.File.ReadAllBytes(attachment.FilePath);
            return File(fileBytes, attachment.ContentType, attachment.FileName);
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

    public class AttachmentUploadModel
    {
        public Microsoft.AspNetCore.Http.IFormFile File { get; set; }
        public string UploaderId { get; set; }
    }
}