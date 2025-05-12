using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoanKhoaServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskSessionController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public TaskSessionController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet]
        public async Task<List<TaskSession>> Get() =>
            await _mongoDBService.GetAllTaskSessionsAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<TaskSession>> Get(string id)
        {
            var taskSession = await _mongoDBService.GetTaskSessionByIdAsync(id);

            if (taskSession is null)
                return NotFound();

            return taskSession;
        }



        // Thêm endpoint mới không yêu cầu Id
        [HttpPost("create")]
        public async Task<ActionResult<TaskSession>> CreateTaskSessionNoId([FromBody] dynamic taskSessionDto)
        {
            try
            {
                // Tạo đối tượng TaskSession mới từ DTO
                var taskSession = new TaskSession
                {
                    // Id = null, MongoDB sẽ tự tạo
                    Name = taskSessionDto.Name,
                    ManagerId = taskSessionDto.ManagerId,
                    ManagerName = taskSessionDto.ManagerName,
                    Type = taskSessionDto.Type,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _mongoDBService.CreateTaskSessionAsync(taskSession);
                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }
        // Sửa phương thức POST để trả về thông tin lỗi chi tiết hơn
        [HttpPost]
        public async Task<ActionResult<TaskSession>> CreateTaskSession([FromBody] CreateTaskSessionDto dto)
        {
            try
            {
                // Tạo đối tượng TaskSession mới từ DTO
                var taskSession = new TaskSession
                {
                    Id = null, // MongoDB sẽ tự sinh ID
                    Name = dto.Name,
                    ManagerId = dto.ManagerId,
                    ManagerName = dto.ManagerName,
                    Type = dto.Type,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _mongoDBService.CreateTaskSessionAsync(taskSession);
                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, TaskSession updatedTaskSession)
        {
            var taskSession = await _mongoDBService.GetTaskSessionByIdAsync(id);

            if (taskSession is null)
                return NotFound();

            await _mongoDBService.UpdateTaskSessionAsync(id, updatedTaskSession);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var taskSession = await _mongoDBService.GetTaskSessionByIdAsync(id);

            if (taskSession is null)
                return NotFound();

            await _mongoDBService.DeleteTaskSessionAsync(id);

            return NoContent();
        }
        [HttpGet("test")]
        public ActionResult<string> Test()
        {
            return "TaskSession API is working!";
        }
    }
}