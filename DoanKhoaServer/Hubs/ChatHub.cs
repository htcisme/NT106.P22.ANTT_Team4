using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace DoanKhoaServer.Hubs
{
    public class ChatHub : Hub
    {
        private readonly MongoDBService _mongoDBService;

        public ChatHub(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string groupId)
        {
            Console.WriteLine($"Client {Context.ConnectionId} joining group {groupId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            await Clients.Caller.SendAsync("JoinedGroup", groupId);
        }

        public async Task LeaveGroup(string groupId)
        {
            Console.WriteLine($"Client {Context.ConnectionId} leaving group {groupId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
            await Clients.Caller.SendAsync("LeftGroup", groupId);
        }

        // SỬA LẠI: Method này CHỈ broadcast, KHÔNG lưu database
        public async Task SendMessage(string conversationId, string senderId, string senderName, string content)
        {
            try
            {
                Console.WriteLine($"Broadcasting message to group {conversationId} from {senderName}: {content}");

                // THAY ĐỔI: Lấy tin nhắn mới nhất từ database (bao gồm attachments)
                var latestMessage = await _mongoDBService.GetLatestMessageForConversation(conversationId);

                if (latestMessage != null)
                {
                    // Gửi full message object (bao gồm attachments) thay vì chỉ gửi text
                    await Clients.OthersInGroup(conversationId).SendAsync("ReceiveMessage", latestMessage);
                    Console.WriteLine($"Broadcasted full message with {latestMessage.Attachments?.Count ?? 0} attachments");
                }
                else
                {
                    // Fallback: tạo message object cơ bản nếu không tìm thấy trong DB
                    var basicMessage = new Message
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        ConversationId = conversationId,
                        SenderId = senderId,
                        Content = content,
                        Timestamp = DateTime.UtcNow,
                        Type = MessageType.Text,
                        IsRead = false,
                        IsEdited = false,
                        Attachments = new List<Attachment>()
                    };

                    await Clients.OthersInGroup(conversationId).SendAsync("ReceiveMessage", basicMessage);
                    Console.WriteLine("Broadcasted basic message (no attachments found)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendMessage: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorSending", ex.Message);
            }
        }

        // Update message
        public async Task UpdateMessage(string messageId, string newContent)
        {
            try
            {
                var message = await _mongoDBService.GetMessageByIdAsync(messageId);
                if (message != null)
                {
                    message.Content = newContent;
                    message.IsEdited = true;

                    var success = await _mongoDBService.UpdateMessageAsync(message);
                    if (success)
                    {
                        // Notify all clients in the conversation
                        await Clients.Group(message.ConversationId).SendAsync("MessageUpdated", messageId, newContent);
                        Console.WriteLine($"Message {messageId} updated and broadcasted");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating message {messageId}: {ex.Message}");
            }
        }

        // Delete message
        public async Task DeleteMessage(string messageId)
        {
            try
            {
                var message = await _mongoDBService.GetMessageByIdAsync(messageId);
                if (message != null)
                {
                    var success = await _mongoDBService.DeleteMessageAsync(messageId);
                    if (success)
                    {
                        // Notify all clients in the conversation
                        await Clients.Group(message.ConversationId).SendAsync("MessageDeleted", messageId);
                        Console.WriteLine($"Message {messageId} deleted and broadcasted");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting message {messageId}: {ex.Message}");
            }
        }
    }
}