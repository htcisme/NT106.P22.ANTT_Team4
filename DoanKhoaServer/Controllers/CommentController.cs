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
    public class CommentController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public CommentController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet("activity/{activityId}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetCommentsByActivityId(string activityId, [FromQuery] string userId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(activityId))
                {
                    return BadRequest("Activity ID is required");
                }

                var comments = await _mongoDBService.GetCommentsByActivityIdAsync(activityId, userId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCommentsByActivityId: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Comment>> CreateComment([FromBody] CreateCommentRequest request)
        {
            try
            {
                // Enhanced validation with detailed logging
                Console.WriteLine($"=== CreateComment Request ===");
                Console.WriteLine($"ActivityId: {request?.ActivityId ?? "null"}");
                Console.WriteLine($"UserId: {request?.UserId ?? "null"}");
                Console.WriteLine($"Content: {request?.Content ?? "null"}");
                Console.WriteLine($"ParentCommentId: {request?.ParentCommentId ?? "null"}");

                if (request == null)
                {
                    Console.WriteLine("ERROR: Request is null");
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    Console.WriteLine("ERROR: Content is empty");
                    return BadRequest("Comment content is required");
                }

                if (string.IsNullOrEmpty(request.ActivityId))
                {
                    Console.WriteLine("ERROR: ActivityId is empty");
                    return BadRequest("Activity ID is required");
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    Console.WriteLine("ERROR: UserId is empty");
                    return BadRequest("User ID is required");
                }

                // FIX: Proper ParentCommentId handling
                string parentCommentId = null;
                if (!string.IsNullOrWhiteSpace(request.ParentCommentId))
                {
                    // Validate ParentCommentId format
                    if (MongoDB.Bson.ObjectId.TryParse(request.ParentCommentId.Trim(), out _))
                    {
                        parentCommentId = request.ParentCommentId.Trim();
                        Console.WriteLine($"Valid ParentCommentId: {parentCommentId}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid ParentCommentId format: {request.ParentCommentId}");
                        return BadRequest("Invalid ParentCommentId format");
                    }
                }
                else
                {
                    Console.WriteLine("ParentCommentId is null or empty - this is a top-level comment");
                }

                // Validate ObjectId formats
                if (!MongoDB.Bson.ObjectId.TryParse(request.ActivityId, out _))
                {
                    Console.WriteLine($"Invalid ActivityId format: {request.ActivityId}");
                    return BadRequest("Invalid ActivityId format");
                }

                if (!MongoDB.Bson.ObjectId.TryParse(request.UserId, out _))
                {
                    Console.WriteLine($"Invalid UserId format: {request.UserId}");
                    return BadRequest("Invalid UserId format");
                }

                var comment = new Comment
                {
                    ActivityId = request.ActivityId.Trim(),
                    UserId = request.UserId.Trim(),
                    Content = request.Content.Trim(),
                    ParentCommentId = parentCommentId, // This can be null
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LikeCount = 0
                };

                Console.WriteLine($"Creating comment object:");
                Console.WriteLine($"- ActivityId: {comment.ActivityId}");
                Console.WriteLine($"- UserId: {comment.UserId}");
                Console.WriteLine($"- ParentCommentId: {comment.ParentCommentId ?? "null"}");
                Console.WriteLine($"- Content length: {comment.Content.Length}");

                var createdComment = await _mongoDBService.CreateCommentAsync(comment);

                Console.WriteLine($"Comment created successfully with ID: {createdComment.Id}");

                return CreatedAtAction(nameof(GetCommentsByActivityId),
                    new { activityId = comment.ActivityId }, createdComment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CreateComment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Return more specific error information
                if (ex.Message.Contains("validation"))
                {
                    return BadRequest($"Validation error: {ex.Message}");
                }
                else if (ex.Message.Contains("duplicate"))
                {
                    return Conflict($"Duplicate entry: {ex.Message}");
                }
                else
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }

        [HttpPut("{commentId}")]
        public async Task<ActionResult<Comment>> UpdateComment(string commentId, [FromBody] UpdateCommentRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(commentId))
                {
                    return BadRequest("Comment ID is required");
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest("Comment content is required");
                }

                var updatedComment = await _mongoDBService.UpdateCommentAsync(commentId, request.Content);
                if (updatedComment == null)
                {
                    return NotFound($"Comment with ID {commentId} not found");
                }

                return Ok(updatedComment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateComment: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(string commentId)
        {
            try
            {
                if (string.IsNullOrEmpty(commentId))
                {
                    return BadRequest("Comment ID is required");
                }

                var success = await _mongoDBService.DeleteCommentAsync(commentId);
                if (!success)
                {
                    return NotFound($"Comment with ID {commentId} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteComment: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("{commentId}/like")]
        public async Task<IActionResult> ToggleCommentLike(string commentId, [FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(commentId))
                {
                    return BadRequest("Comment ID is required");
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }

                var result = await _mongoDBService.ToggleCommentLikeAsync(commentId, userId);
                if (!result)
                {
                    return BadRequest("Unable to toggle comment like status");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleCommentLike: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("user-status/{userId}")]
        public async Task<ActionResult<Dictionary<string, bool>>> GetUserCommentStatus(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }

                var statuses = await _mongoDBService.GetUserCommentStatusesAsync(userId);
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserCommentStatus: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("test")]
        public IActionResult TestConnection()
        {
            try
            {
                Console.WriteLine("Comment API test endpoint called");
                return Ok(new
                {
                    message = "Comment API is working",
                    timestamp = DateTime.UtcNow,
                    status = "success"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in test endpoint: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "Comment API test failed",
                    error = ex.Message,
                    status = "error"
                });
            }
        }
    }

    // Request models - FIX: Make ParentCommentId nullable
    public class CreateCommentRequest
    {
        public string ActivityId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public string? ParentCommentId { get; set; } = null; // Make nullable with default null
    }

    public class UpdateCommentRequest
    {
        public string Content { get; set; }
    }
}