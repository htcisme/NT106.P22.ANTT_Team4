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
    public class TaskProgramController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public TaskProgramController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet]
        public async Task<List<TaskProgram>> Get() =>
            await _mongoDBService.GetAllTaskProgramsAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<TaskProgram>> Get(string id)
        {
            var taskProgram = await _mongoDBService.GetTaskProgramByIdAsync(id);

            if (taskProgram is null)
                return NotFound();

            return taskProgram;
        }
        
        [HttpGet("session/{sessionId}")]
        public async Task<List<TaskProgram>> GetBySessionId(string sessionId) =>
            await _mongoDBService.GetTaskProgramsBySessionIdAsync(sessionId);

        [HttpPost]
        public async Task<ActionResult<TaskProgram>> Post(TaskProgram taskProgram)
        {
            try
            {
                // If no ID is provided, generate a MongoDB ObjectId
                if (string.IsNullOrEmpty(taskProgram.Id))
                {
                    taskProgram.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                }

                await _mongoDBService.CreateTaskProgramAsync(taskProgram);
                return CreatedAtAction(nameof(Get), new { id = taskProgram.Id }, taskProgram);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating TaskProgram: {ex.Message}");
            }
        }
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, TaskProgram updatedTaskProgram)
        {
            var taskProgram = await _mongoDBService.GetTaskProgramByIdAsync(id);

            if (taskProgram is null)
                return NotFound();

            await _mongoDBService.UpdateTaskProgramAsync(id, updatedTaskProgram);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var taskProgram = await _mongoDBService.GetTaskProgramByIdAsync(id);

            if (taskProgram is null)
                return NotFound();

            await _mongoDBService.DeleteTaskProgramAsync(id);

            return NoContent();
        }
    }
}