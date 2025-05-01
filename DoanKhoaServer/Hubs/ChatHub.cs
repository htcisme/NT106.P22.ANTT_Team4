using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task SendMessage(string conversationId, string senderId, string content)
        {
            try
            {
                var message = new Message
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    Type = MessageType.Text
                };

                // Lưu tin nhắn vào database
                await _mongoDBService.CreateMessageAsync(message);

                // Gửi tin nhắn đến tất cả client trong group
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendMessage: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorSending", ex.Message);
            }
        }

        public async Task MarkAsRead(string messageId, string userId)
        {
            // Implement this if needed
        }

        public async Task SendMessageWithAttachments(string conversationId, string senderId, string content, List<Attachment> attachments, MessageType messageType)
        {
            try
            {
                var message = new Message
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    Type = messageType,
                    Attachments = attachments ?? new List<Attachment>()
                };

                // Lưu tin nhắn vào database
                await _mongoDBService.CreateMessageWithAttachmentsAsync(message);

                // Gửi tin nhắn đến tất cả client trong nhóm
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", message);

                // Gửi xác nhận cho người gửi
                await Clients.Caller.SendAsync("MessageSent", true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendMessageWithAttachments: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorSending", ex.Message);
            }
        }

        public async Task CreateGroupConversation(Conversation conversation)
        {
            try
            {
                // Đảm bảo conversation là group
                conversation.IsGroup = true;
                conversation.LastActivity = DateTime.UtcNow;

                // Thêm người tạo vào danh sách admin
                if (!conversation.GroupMembers.Any(m => m.UserId == conversation.CreatorId))
                {
                    conversation.GroupMembers.Add(new GroupMember
                    {
                        UserId = conversation.CreatorId,
                        Role = GroupRole.Owner,
                        JoinedAt = DateTime.UtcNow
                    });
                }

                // Lưu vào database
                var savedConversation = await _mongoDBService.CreateGroupConversationAsync(conversation);

                // Thông báo cho tất cả thành viên về nhóm mới
                foreach (var userId in conversation.ParticipantIds)
                {
                    // Add user to SignalR group
                    // In reality, you would need to find their connection by userId
                    // This is simplified here
                    await Groups.AddToGroupAsync(Context.ConnectionId, savedConversation.Id);
                }

                // Tạo tin nhắn thông báo về việc nhóm được tạo
                var systemMessage = new Message
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ConversationId = savedConversation.Id,
                    SenderId = conversation.CreatorId,
                    Content = $"Group '{conversation.Title}' has been created",
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    Type = MessageType.System
                };

                await _mongoDBService.CreateMessageAsync(systemMessage);
                await Clients.Group(savedConversation.Id).SendAsync("ReceiveMessage", systemMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating group conversation: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorCreatingGroup", ex.Message);
            }
        }

        public async Task AddUserToGroup(string conversationId, string userId, GroupRole role = GroupRole.Member)
        {
            // Kiểm tra quyền (chỉ owner hoặc admin mới được thêm thành viên)
            var conversation = await _mongoDBService.GetConversationByIdAsync(conversationId);
            if (conversation == null || !conversation.IsGroup)
            {
                await Clients.Caller.SendAsync("ErrorAddingUser", "Group not found");
                return;
            }

            var callerUserId = Context.UserIdentifier; // Cần setup authentication đúng cách
            var callerMember = conversation.GroupMembers.FirstOrDefault(m => m.UserId == callerUserId);
            if (callerMember == null || (callerMember.Role != GroupRole.Owner && callerMember.Role != GroupRole.Admin))
            {
                await Clients.Caller.SendAsync("ErrorAddingUser", "You don't have permission to add members");
                return;
            }

            // Thêm người dùng vào nhóm
            var success = await _mongoDBService.AddUserToGroupAsync(conversationId, userId, role);
            if (success)
            {
                // Thông báo cho tất cả thành viên về việc có người mới tham gia
                var user = await _mongoDBService.GetUserByIdAsync(userId);
                var systemMessage = new Message
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ConversationId = conversationId,
                    SenderId = callerUserId,
                    Content = $"{user?.DisplayName ?? userId} has joined the group",
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    Type = MessageType.System
                };

                await _mongoDBService.CreateMessageAsync(systemMessage);
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", systemMessage);

                // Notify about the updated group
                var updatedConversation = await _mongoDBService.GetConversationByIdAsync(conversationId);
                await Clients.Group(conversationId).SendAsync("ConversationUpdated", updatedConversation);
            }
            else
            {
                await Clients.Caller.SendAsync("ErrorAddingUser", "Failed to add user to group");
            }
        }
    }
}