using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DoanKhoaServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityManagementController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public ActivityManagementController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        /// <summary>
        /// Lấy danh sách người tham gia hoạt động
        /// </summary>
        [HttpGet("{activityId}/participants")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetParticipants(string activityId)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId))
                {
                    return BadRequest("Activity ID is required");
                }

                var participants = await _mongoDBService.GetDetailedParticipantsByActivityIdAsync(activityId);
                return Ok(participants);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetParticipants: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy danh sách người thích hoạt động
        /// </summary>
        [HttpGet("{activityId}/likes")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLikes(string activityId)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId))
                {
                    return BadRequest("Activity ID is required");
                }

                var likes = await _mongoDBService.GetDetailedLikesByActivityIdAsync(activityId);
                return Ok(likes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLikes: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa người tham gia khỏi hoạt động
        /// </summary>
        [HttpDelete("{activityId}/participants/{userId}")]
        public async Task<IActionResult> RemoveParticipant(string activityId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Activity ID and User ID are required");
                }

                var success = await _mongoDBService.RemoveParticipantFromActivityAsync(activityId, userId);

                if (success)
                {
                    return Ok(new { message = "Participant removed successfully" });
                }
                else
                {
                    return NotFound("Participant not found or not participating in this activity");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveParticipant: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa lượt thích khỏi hoạt động
        /// </summary>
        [HttpDelete("{activityId}/likes/{userId}")]
        public async Task<IActionResult> RemoveLike(string activityId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Activity ID and User ID are required");
                }

                var success = await _mongoDBService.RemoveLikeFromActivityAsync(activityId, userId);

                if (success)
                {
                    return Ok(new { message = "Like removed successfully" });
                }
                else
                {
                    return NotFound("Like not found for this user and activity");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveLike: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy thống kê chi tiết hoạt động
        /// </summary>
        [HttpGet("{activityId}/statistics")]
        public async Task<ActionResult<dynamic>> GetActivityStatistics(string activityId)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId))
                {
                    return BadRequest("Activity ID is required");
                }

                var statistics = await _mongoDBService.GetActivityStatisticsAsync(activityId);

                if (statistics == null)
                {
                    return NotFound($"Activity with ID {activityId} not found");
                }

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActivityStatistics: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa hàng loạt hoạt động
        /// </summary>
        [HttpPost("bulk-delete")]
        public async Task<IActionResult> BulkDeleteActivities([FromBody] BulkDeleteRequest request)
        {
            try
            {
                if (request?.ActivityIds == null || request.ActivityIds.Count == 0)
                {
                    return BadRequest("Activity IDs are required");
                }

                var success = await _mongoDBService.BulkDeleteActivitiesAsync(request.ActivityIds);

                if (success)
                {
                    return Ok(new { message = $"Successfully deleted {request.ActivityIds.Count} activities" });
                }
                else
                {
                    return StatusCode(500, "Failed to delete some activities");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkDeleteActivities: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Thêm người tham gia vào hoạt động (dành cho admin)
        /// </summary>
        [HttpPost("{activityId}/participants/{userId}")]
        public async Task<IActionResult> AddParticipant(string activityId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Activity ID and User ID are required");
                }

                // Kiểm tra hoạt động có tồn tại không
                var activity = await _mongoDBService.GetActivityByIdAsync(activityId);
                if (activity == null)
                {
                    return NotFound("Activity not found");
                }

                // Kiểm tra người dùng có tồn tại không
                var user = await _mongoDBService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                var success = await _mongoDBService.ToggleActivityParticipationAsync(activityId, userId);

                if (success)
                {
                    return Ok(new { message = "Participant added successfully" });
                }
                else
                {
                    return BadRequest("Failed to add participant");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddParticipant: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy báo cáo tổng quan hoạt động
        /// </summary>
        [HttpGet("overview-report")]
        public async Task<ActionResult<dynamic>> GetOverviewReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var activities = await _mongoDBService.GetActivitiesAsync();

                // Lọc theo ngày nếu có
                if (startDate.HasValue)
                {
                    activities = activities.Where(a => a.Date >= startDate.Value).ToList();
                }

                if (endDate.HasValue)
                {
                    activities = activities.Where(a => a.Date <= endDate.Value).ToList();
                }

                var report = new
                {
                    TotalActivities = activities.Count,
                    TotalParticipants = activities.Sum(a => a.ParticipantCount ?? 0),
                    TotalLikes = activities.Sum(a => a.LikeCount ?? 0),
                    ActivitiesByType = activities.GroupBy(a => a.Type)
                        .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                        .ToList(),
                    ActivitiesByStatus = activities.GroupBy(a => a.Status)
                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                        .ToList(),
                    TopActivitiesByParticipants = activities
                        .OrderByDescending(a => a.ParticipantCount ?? 0)
                        .Take(5)
                        .Select(a => new {
                            a.Id,
                            a.Title,
                            ParticipantCount = a.ParticipantCount ?? 0,
                            LikeCount = a.LikeCount ?? 0,
                            a.Date
                        })
                        .ToList(),
                    TopActivitiesByLikes = activities
                        .OrderByDescending(a => a.LikeCount ?? 0)
                        .Take(5)
                        .Select(a => new {
                            a.Id,
                            a.Title,
                            ParticipantCount = a.ParticipantCount ?? 0,
                            LikeCount = a.LikeCount ?? 0,
                            a.Date
                        })
                        .ToList(),
                    RecentActivities = activities
                        .OrderByDescending(a => a.CreatedAt)
                        .Take(10)
                        .Select(a => new {
                            a.Id,
                            a.Title,
                            a.Status,
                            a.Date,
                            a.CreatedAt,
                            ParticipantCount = a.ParticipantCount ?? 0,
                            LikeCount = a.LikeCount ?? 0
                        })
                        .ToList(),
                    PeriodStart = startDate ?? activities.Min(a => a.Date),
                    PeriodEnd = endDate ?? activities.Max(a => a.Date)
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOverviewReport: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy chi tiết hoạt động với thông tin mở rộng
        /// </summary>
        [HttpGet("{activityId}/detailed")]
        public async Task<ActionResult<dynamic>> GetDetailedActivity(string activityId)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId))
                {
                    return BadRequest("Activity ID is required");
                }

                var activity = await _mongoDBService.GetActivityByIdAsync(activityId);
                if (activity == null)
                {
                    return NotFound($"Activity with ID {activityId} not found");
                }

                var participants = await _mongoDBService.GetDetailedParticipantsByActivityIdAsync(activityId);
                var likes = await _mongoDBService.GetDetailedLikesByActivityIdAsync(activityId);
                var comments = await _mongoDBService.GetCommentsByActivityIdAsync(activityId);

                var detailedActivity = new
                {
                    Activity = activity,
                    ParticipantsCount = participants.Count,
                    LikesCount = likes.Count,
                    CommentsCount = comments.Count,
                    Participants = participants,
                    LikedUsers = likes,
                    RecentComments = comments.OrderByDescending(c => c.CreatedAt).Take(5),
                    Statistics = new
                    {
                        EngagementRate = activity.ParticipantCount > 0
                            ? Math.Round((double)(activity.LikeCount ?? 0) / (activity.ParticipantCount ?? 1) * 100, 2)
                            : 0,
                        CommentsPerParticipant = activity.ParticipantCount > 0
                            ? Math.Round((double)comments.Count / (activity.ParticipantCount ?? 1), 2)
                            : 0,
                        DaysActive = (DateTime.UtcNow - activity.CreatedAt).Days,
                        IsPopular = (activity.ParticipantCount ?? 0) > 10 || (activity.LikeCount ?? 0) > 20
                    }
                };

                return Ok(detailedActivity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDetailedActivity: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật hàng loạt trạng thái hoạt động
        /// </summary>
        [HttpPut("bulk-update-status")]
        public async Task<IActionResult> BulkUpdateActivityStatus([FromBody] BulkUpdateStatusRequest request)
        {
            try
            {
                if (request?.ActivityIds == null || request.ActivityIds.Count == 0)
                {
                    return BadRequest("Activity IDs are required");
                }

                if (!Enum.IsDefined(typeof(ActivityStatus), request.NewStatus))
                {
                    return BadRequest("Invalid activity status");
                }

                int updatedCount = 0;
                foreach (var activityId in request.ActivityIds)
                {
                    var activity = await _mongoDBService.GetActivityByIdAsync(activityId);
                    if (activity != null)
                    {
                        activity.Status = request.NewStatus;
                        await _mongoDBService.UpdateActivityAsync(activityId, activity);
                        updatedCount++;
                    }
                }

                return Ok(new
                {
                    message = $"Successfully updated {updatedCount} activities",
                    updatedCount = updatedCount,
                    totalRequested = request.ActivityIds.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateActivityStatus: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Xuất dữ liệu hoạt động ra CSV
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportActivities([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var activities = await _mongoDBService.GetActivitiesAsync();

                // Lọc theo ngày nếu có
                if (startDate.HasValue)
                {
                    activities = activities.Where(a => a.Date >= startDate.Value).ToList();
                }

                if (endDate.HasValue)
                {
                    activities = activities.Where(a => a.Date <= endDate.Value).ToList();
                }

                var csvContent = new StringBuilder();
                csvContent.AppendLine("ID,Title,Type,Status,Date,CreatedAt,ParticipantCount,LikeCount,Description");

                foreach (var activity in activities)
                {
                    csvContent.AppendLine($"{activity.Id}," +
                        $"\"{activity.Title}\"," +
                        $"{activity.Type}," +
                        $"{activity.Status}," +
                        $"{activity.Date:yyyy-MM-dd}," +
                        $"{activity.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                        $"{activity.ParticipantCount ?? 0}," +
                        $"{activity.LikeCount ?? 0}," +
                        $"\"{activity.Description?.Replace("\"", "\"\"")}\"");
                }

                var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
                var fileName = $"activities_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExportActivities: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    // Request models
    public class BulkDeleteRequest
    {
        public List<string> ActivityIds { get; set; } = new List<string>();
    }

    public class BulkUpdateStatusRequest
    {
        public List<string> ActivityIds { get; set; } = new List<string>();
        public ActivityStatus NewStatus { get; set; }
    }
}