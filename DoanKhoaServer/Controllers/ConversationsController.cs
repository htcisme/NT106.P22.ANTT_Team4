using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoanKhoaServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public ConversationsController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserConversations(string userId)
        {
            try
            {
                var conversations = await _mongoDBService.GetConversationsByUserIdAsync(userId);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpPost("private")]
        public async Task<IActionResult> CreatePrivateConversation([FromBody] PrivateConversationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.CurrentUserId))
                {
                    return BadRequest("Both UserId and CurrentUserId are required");
                }

                if (request.CurrentUserId == request.UserId)
                {
                    return BadRequest("Cannot create conversation with yourself");
                }

                // Check if both users exist
                var currentUser = await _mongoDBService.GetUserByIdAsync(request.CurrentUserId);
                var otherUser = await _mongoDBService.GetUserByIdAsync(request.UserId);

                if (currentUser == null || otherUser == null)
                {
                    return BadRequest("One or more users not found");
                }

                // Check if a private conversation already exists between these users
                var existingConversation = await _mongoDBService.GetPrivateConversationAsync(request.CurrentUserId, request.UserId);

                if (existingConversation != null)
                {
                    return Ok(existingConversation);
                }

                // Create new conversation
                var conversation = new Conversation
                {
                    Title = otherUser.DisplayName, // For current user, show other user's name
                    ParticipantIds = new List<string> { request.CurrentUserId, request.UserId },
                    IsGroup = false,
                    LastActivity = DateTime.UtcNow
                };

                var savedConversation = await _mongoDBService.CreateConversationAsync(conversation);

                // Add conversation to both users' conversation lists
                await _mongoDBService.AddConversationToUserAsync(request.CurrentUserId, savedConversation.Id);
                await _mongoDBService.AddConversationToUserAsync(request.UserId, savedConversation.Id);

                return Ok(savedConversation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetMessagesByConversationId(string conversationId)
        {
            try
            {
                var messages = await _mongoDBService.GetMessagesByConversationIdAsync(conversationId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
    }

    public class PrivateConversationRequest
    {
        public string UserId { get; set; }
        public string CurrentUserId { get; set; }
    }
}