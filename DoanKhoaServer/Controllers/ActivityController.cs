using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DoanKhoaServer.Controllers
{
    using Activity = DoanKhoaServer.Models.Activity;

    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public ActivityController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Activity>>> GetActivities()
        {
            try
            {
                var activities = await _mongoDBService.GetActivitiesAsync();
                return Ok(activities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Activity>> CreateActivity(Activity activity)
        {
            if (activity == null || string.IsNullOrWhiteSpace(activity.Title))
            {
                return BadRequest("Title is required.");
            }

            try
            {
                await _mongoDBService.CreateActivityAsync(activity);
                return CreatedAtAction(nameof(GetActivities), new { id = activity.Id }, activity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateActivity(string id, Activity activity)
        {
            try
            {
                if (id != activity.Id)
                    return BadRequest();

                await _mongoDBService.UpdateActivityAsync(id, activity);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(string id)
        {
            try
            {
                await _mongoDBService.DeleteActivityAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
