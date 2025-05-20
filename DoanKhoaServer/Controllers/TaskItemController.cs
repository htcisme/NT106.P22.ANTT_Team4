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
    public class TaskItemController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public TaskItemController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet]
        public async Task<List<TaskItem>> Get() =>
            await _mongoDBService.GetAllTaskItemsAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<TaskItem>> Get(string id)
        {
            var taskItem = await _mongoDBService.GetTaskItemByIdAsync(id);

            if (taskItem is null)
                return NotFound();

            return taskItem;
        }

        [HttpGet("program/{programId}")]
        public async Task<List<TaskItem>> GetByProgramId(string programId) =>
            await _mongoDBService.GetTaskItemsByProgramIdAsync(programId);

        [HttpPost]
        public async Task<ActionResult<TaskItem>> Post(TaskItem taskItem)
        {
            try
            {
                await _mongoDBService.CreateTaskItemAsync(taskItem);
                return CreatedAtAction(nameof(Get), new { id = taskItem.Id }, taskItem);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, TaskItem updatedTaskItem)
        {
            var taskItem = await _mongoDBService.GetTaskItemByIdAsync(id);

            if (taskItem is null)
                return NotFound();

            await _mongoDBService.UpdateTaskItemAsync(id, updatedTaskItem);

            return NoContent();
        }

        [HttpPut("{id:length(24)}/complete")]
        public async Task<ActionResult<TaskItem>> Complete(string id)
        {
            var taskItem = await _mongoDBService.CompleteTaskItemAsync(id);

            if (taskItem is null)
                return NotFound();

            return taskItem;
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var taskItem = await _mongoDBService.GetTaskItemByIdAsync(id);

            if (taskItem is null)
                return NotFound();

            await _mongoDBService.DeleteTaskItemAsync(id);

            return NoContent();
        }
    }
}