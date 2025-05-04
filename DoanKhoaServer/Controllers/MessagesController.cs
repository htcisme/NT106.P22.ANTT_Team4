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
    public class MessagesController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public MessagesController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessageById(string id)
        {
            try
            {
                var message = await _mongoDBService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound("Message not found");
                }
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
    }
}