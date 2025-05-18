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
        public async Task<ActionResult<IEnumerable<dynamic>>> GetActivities([FromQuery] string userId = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    // Lấy danh sách hoạt động kèm trạng thái người dùng
                    var activitiesWithStatus = await _mongoDBService.GetActivitiesWithUserStatusAsync(userId);
                    return Ok(activitiesWithStatus);
                }
                else
                {
                    // Lấy danh sách hoạt động thông thường
                    var activities = await _mongoDBService.GetActivitiesAsync();
                    return Ok(activities);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActivities: {ex.Message}");
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
                // Đảm bảo các trường mới có giá trị mặc định
                activity.ParticipantCount = 0;
                activity.LikeCount = 0;

                await _mongoDBService.CreateActivityAsync(activity);
                return CreatedAtAction(nameof(GetActivities), new { id = activity.Id }, activity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateActivity: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateActivity(string id, Activity activity)
        {
            try
            {
                if (id != activity.Id)
                    return BadRequest("ID mismatch between route and body");

                // Lấy hoạt động hiện tại để đảm bảo giữ nguyên các trường quan trọng
                var existingActivity = await _mongoDBService.GetActivityByIdAsync(id);
                if (existingActivity == null)
                    return NotFound($"Activity with ID {id} not found");

                // Giữ nguyên số người tham gia và số lượt thích
                activity.ParticipantCount = existingActivity.ParticipantCount;
                activity.LikeCount = existingActivity.LikeCount;

                await _mongoDBService.UpdateActivityAsync(id, activity);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateActivity: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(string id)
        {
            try
            {
                // Xóa hoạt động
                await _mongoDBService.DeleteActivityAsync(id);

                // Xóa tất cả trạng thái người dùng liên quan đến hoạt động này
                await _mongoDBService.DeleteUserActivityStatusesByActivityAsync(id);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteActivity: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // THÊM CÁC ENDPOINT MỚI

        // 1. Toggle tham gia hoạt động
        [HttpPost("{id}/participate")]
        public async Task<IActionResult> ToggleParticipation(string id, [FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("UserId is required");
            }

            try
            {
                var result = await _mongoDBService.ToggleActivityParticipationAsync(id, userId);
                if (result)
                {
                    return Ok(new { success = true });
                }

                return BadRequest(new { message = "Unable to toggle participation status" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleParticipation: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // 2. Toggle yêu thích hoạt động
        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(string id, [FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("UserId is required");
            }

            try
            {
                var result = await _mongoDBService.ToggleActivityLikeAsync(id, userId);
                if (result)
                {
                    return Ok(new { success = true });
                }

                return BadRequest(new { message = "Unable to toggle like status" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleLike: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // 3. Lấy trạng thái hoạt động của người dùng
        [HttpGet("user-status/{userId}")]
        public async Task<ActionResult<Dictionary<string, bool>>> GetUserActivityStatus(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("UserId is required");
            }

            try
            {
                var statuses = await _mongoDBService.GetUserActivityStatusesAsync(userId);
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserActivityStatus: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}