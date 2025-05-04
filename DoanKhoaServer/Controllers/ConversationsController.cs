using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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


        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupConversation([FromBody] Conversation conversation)
        {
            try
            {
                if (string.IsNullOrEmpty(conversation.Title))
                {
                    return BadRequest("Group name is required");
                }

                if (string.IsNullOrEmpty(conversation.CreatorId))
                {
                    return BadRequest("Creator ID is required");
                }

                if (conversation.ParticipantIds == null || conversation.ParticipantIds.Count < 2)
                {
                    return BadRequest("At least 2 participants are required");
                }

                // Set required fields if they're missing
                if (string.IsNullOrEmpty(conversation.Id))
                {
                    conversation.Id = ObjectId.GenerateNewId().ToString();
                }

                conversation.LastMessageId = conversation.LastMessageId ?? string.Empty;

                // Create the conversation
                var createdConversation = await _mongoDBService.CreateGroupConversationAsync(conversation);

                // Create a system message indicating group creation
                var systemMessage = new Message
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ConversationId = createdConversation.Id,
                    SenderId = conversation.CreatorId,
                    Content = $"Group '{conversation.Title}' has been created",
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.System
                };

                await _mongoDBService.CreateMessageAsync(systemMessage);

                return Ok(createdConversation);
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