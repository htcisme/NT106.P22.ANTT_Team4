using DoanKhoaServer.Models;
using DoanKhoaServer.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoanKhoaServer.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Message> _messagesCollection;
        private readonly IMongoCollection<Conversation> _conversationsCollection;
        private readonly IMongoCollection<Attachment> _attachmentsCollection;

        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var mongoClient = new MongoClient(
                mongoDBSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                mongoDBSettings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<User>(
                mongoDBSettings.Value.UsersCollectionName);

            _messagesCollection = mongoDatabase.GetCollection<Message>(
                mongoDBSettings.Value.MessagesCollectionName);

            _conversationsCollection = mongoDatabase.GetCollection<Conversation>(
                mongoDBSettings.Value.ConversationsCollectionName);

            _attachmentsCollection = mongoDatabase.GetCollection<Attachment>(
                mongoDBSettings.Value.AttachmentsCollectionName);
        }

        // Users methods
        public async Task<List<User>> GetAllUsersAsync() =>
            await _usersCollection.Find(_ => true).ToListAsync();

        public async Task<User> GetUserByIdAsync(string id) =>
            await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateUserAsync(User user) =>
            await _usersCollection.InsertOneAsync(user);

        // Messages methods
        public async Task<List<Message>> GetMessagesByConversationIdAsync(string conversationId) =>
            await _messagesCollection.Find(x => x.ConversationId == conversationId)
                .SortBy(m => m.Timestamp)
                .ToListAsync();

        public async Task<Message> CreateMessageAsync(Message message)
        {
            await _messagesCollection.InsertOneAsync(message);

            // Update conversation last activity
            var update = Builders<Conversation>.Update
                .Set(c => c.LastMessageId, message.Id)
                .Set(c => c.LastActivity, message.Timestamp);

            await _conversationsCollection.UpdateOneAsync(
                c => c.Id == message.ConversationId, update);

            return message;
        }

        // Conversations methods
        public async Task<List<Conversation>> GetConversationsByUserIdAsync(string userId) =>
            await _conversationsCollection.Find(x => x.ParticipantIds.Contains(userId))
                .SortByDescending(c => c.LastActivity)
                .ToListAsync();

        public async Task<Conversation> GetConversationByIdAsync(string id) =>
            await _conversationsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<Conversation> CreateConversationAsync(Conversation conversation)
        {
            await _conversationsCollection.InsertOneAsync(conversation);
            return conversation;
        }

        // Attachments methods
        public async Task<Message> CreateMessageWithAttachmentsAsync(Message message)
        {
            // Lưu tin nhắn
            await _messagesCollection.InsertOneAsync(message);

            // Cập nhật conversation với LastActivity và LastMessageId
            var update = Builders<Conversation>.Update
                .Set(c => c.LastActivity, message.Timestamp)
                .Set(c => c.LastMessageId, message.Id);

            await _conversationsCollection.UpdateOneAsync(c => c.Id == message.ConversationId, update);

            return message;
        }

        public async Task<Attachment> SaveAttachmentAsync(Attachment attachment)
        {
            await _attachmentsCollection.InsertOneAsync(attachment);
            return attachment;
        }

        public async Task<Attachment> GetAttachmentByIdAsync(string id)
        {
            return await _attachmentsCollection.Find(a => a.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Attachment>> GetAttachmentsByMessageIdAsync(string messageId)
        {
            return await _attachmentsCollection.Find(a => a.MessageId == messageId).ToListAsync();
        }

        public async Task<List<Attachment>> GetAttachmentsByIdsAsync(List<string> attachmentIds)
        {
            var filter = Builders<Attachment>.Filter.In(a => a.Id, attachmentIds);
            return await _attachmentsCollection.Find(filter).ToListAsync();
        }

        public async Task<Conversation> CreateGroupConversationAsync(Conversation conversation)
        {
            await _conversationsCollection.InsertOneAsync(conversation);

            // Cập nhật danh sách conversations của mỗi thành viên
            foreach (var userId in conversation.ParticipantIds)
            {
                var update = Builders<User>.Update.AddToSet(u => u.Conversations, conversation.Id);
                await _usersCollection.UpdateOneAsync(u => u.Id == userId, update);
            }

            return conversation;
        }

        public async Task<bool> AddUserToGroupAsync(string conversationId, string userId, GroupRole role = GroupRole.Member)
        {
            // Kiểm tra conversation có tồn tại và là group không
            var conversation = await _conversationsCollection.Find(c => c.Id == conversationId && c.IsGroup).FirstOrDefaultAsync();
            if (conversation == null)
                return false;

            // Thêm người dùng vào nhóm
            var updateConv = Builders<Conversation>.Update
                .AddToSet(c => c.ParticipantIds, userId)
                .AddToSet(c => c.GroupMembers, new GroupMember
                {
                    UserId = userId,
                    Role = role,
                    JoinedAt = DateTime.UtcNow
                });

            var resultConv = await _conversationsCollection.UpdateOneAsync(c => c.Id == conversationId, updateConv);

            // Thêm conversation vào danh sách của người dùng
            var updateUser = Builders<User>.Update.AddToSet(u => u.Conversations, conversationId);
            var resultUser = await _usersCollection.UpdateOneAsync(u => u.Id == userId, updateUser);

            return resultConv.ModifiedCount > 0 && resultUser.ModifiedCount > 0;
        }

        public async Task<bool> RemoveUserFromGroupAsync(string conversationId, string userId)
        {
            // Kiểm tra không cho phép xóa owner
            var conversation = await _conversationsCollection.Find(
                c => c.Id == conversationId &&
                c.IsGroup &&
                !c.GroupMembers.Any(m => m.UserId == userId && m.Role == GroupRole.Owner)
            ).FirstOrDefaultAsync();

            if (conversation == null)
                return false;

            // Xóa người dùng khỏi nhóm
            var updateConv = Builders<Conversation>.Update
                .Pull(c => c.ParticipantIds, userId)
                .PullFilter(c => c.GroupMembers, m => m.UserId == userId);

            var resultConv = await _conversationsCollection.UpdateOneAsync(c => c.Id == conversationId, updateConv);

            // Xóa conversation khỏi danh sách của người dùng
            var updateUser = Builders<User>.Update.Pull(u => u.Conversations, conversationId);
            var resultUser = await _usersCollection.UpdateOneAsync(u => u.Id == userId, updateUser);

            return resultConv.ModifiedCount > 0;
        }
        public async Task<Attachment> GetAttachmentAsync(string id)
        {
            // This is just an alias for GetAttachmentByIdAsync for backward compatibility
            return await GetAttachmentByIdAsync(id);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _usersCollection.Find(x => x.Username == username).FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _usersCollection.Find(x => x.Email == email).FirstOrDefaultAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            await _usersCollection.ReplaceOneAsync(x => x.Id == user.Id, user);
        }
    }
}