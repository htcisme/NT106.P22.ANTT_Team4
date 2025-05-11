using DoanKhoaServer.Models;
using DoanKhoaServer.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
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
        private readonly IMongoCollection<Activity> _activitiesCollection;

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

            _activitiesCollection = mongoDatabase.GetCollection<Activity>(
                mongoDBSettings.Value.ActivitiesCollectionName);

            // Các dòng code hiện tại

            _taskSessionsCollection = mongoDatabase.GetCollection<TaskSession>(
                mongoDBSettings.Value.TaskSessionsCollectionName);

            _taskProgramsCollection = mongoDatabase.GetCollection<TaskProgram>(
                mongoDBSettings.Value.TaskProgramsCollectionName);

            _taskItemsCollection = mongoDatabase.GetCollection<TaskItem>(
                mongoDBSettings.Value.TaskItemsCollectionName);
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
            // Ensure it's marked as a group
            conversation.IsGroup = true;
            conversation.LastActivity = DateTime.UtcNow;

            // Generate ID if not provided
            if (string.IsNullOrEmpty(conversation.Id))
            {
                conversation.Id = ObjectId.GenerateNewId().ToString();
            }

            // Add to database
            await _conversationsCollection.InsertOneAsync(conversation);

            // Add this conversation to each participant's list
            foreach (var participantId in conversation.ParticipantIds)
            {
                await AddConversationToUserAsync(participantId, conversation.Id);
            }

            return conversation;
        }

        public async Task AddConversationToUserAsync(string userId, string conversationId)
        {
            var update = Builders<User>.Update.AddToSet(u => u.Conversations, conversationId);
            await _usersCollection.UpdateOneAsync(u => u.Id == userId, update);
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
        public async Task<List<User>> SearchUsersAsync(string searchTerm)
        {
            var filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.Username, new BsonRegularExpression(searchTerm, "i")),
                Builders<User>.Filter.Regex(u => u.DisplayName, new BsonRegularExpression(searchTerm, "i")),
                Builders<User>.Filter.Regex(u => u.Email, new BsonRegularExpression(searchTerm, "i"))
            );

            return await _usersCollection.Find(filter)
                .Limit(20) // Limit số lượng kết quả trả về
                .ToListAsync();
        }

        public async Task<Conversation> GetPrivateConversationAsync(string userId1, string userId2)
        {
            var filter = Builders<Conversation>.Filter.And(
                Builders<Conversation>.Filter.Eq(c => c.IsGroup, false),
                Builders<Conversation>.Filter.All(c => c.ParticipantIds, new[] { userId1, userId2 }),
                Builders<Conversation>.Filter.Size(c => c.ParticipantIds, 2)
            );

            return await _conversationsCollection.Find(filter).FirstOrDefaultAsync();
        }


        public async Task<Message> GetMessageByIdAsync(string id)
        {
            return await _messagesCollection.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> AddUserToGroupAsync(string conversationId, string userId, GroupRole role = GroupRole.Member)
        {
            try
            {
                // Check if conversation exists and is a group
                var conversation = await _conversationsCollection.Find(c => c.Id == conversationId && c.IsGroup).FirstOrDefaultAsync();
                if (conversation == null)
                {
                    return false;
                }

                // Check if user is already in the group
                if (conversation.ParticipantIds.Contains(userId))
                {
                    return false; // User is already in the group
                }

                // Add user to ParticipantIds
                var updateParticipants = Builders<Conversation>.Update.AddToSet(c => c.ParticipantIds, userId);
                await _conversationsCollection.UpdateOneAsync(c => c.Id == conversationId, updateParticipants);

                // Add GroupMember entry
                var groupMember = new GroupMember
                {
                    UserId = userId,
                    Role = role,
                    JoinedAt = DateTime.UtcNow
                };

                var updateGroupMembers = Builders<Conversation>.Update.AddToSet(c => c.GroupMembers, groupMember);
                await _conversationsCollection.UpdateOneAsync(c => c.Id == conversationId, updateGroupMembers);

                // Add conversation to user's conversation list
                await AddConversationToUserAsync(userId, conversationId);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding user to group: {ex.Message}");
                return false;
            }
        }

        //Activities method
        public async Task<List<Activity>> GetActivitiesAsync()
        {
            return await _activitiesCollection.Find(_ => true).ToListAsync();
        }

        public async Task CreateActivityAsync(Activity activity)
        {
            await _activitiesCollection.InsertOneAsync(activity);
        }

        public async Task UpdateActivityAsync(string id, Activity activity)
        {
            activity.Id = id;
            await _activitiesCollection.ReplaceOneAsync(x => x.Id == id, activity);
        }


        public async Task DeleteActivityAsync(string id)
        {
            await _activitiesCollection.DeleteOneAsync(x => x.Id == id);
        }

        // Thêm các dòng sau vào MongoDBService.cs

        private readonly IMongoCollection<TaskSession> _taskSessionsCollection;
        private readonly IMongoCollection<TaskProgram> _taskProgramsCollection;
        private readonly IMongoCollection<TaskItem> _taskItemsCollection;



        // Task Session methods
        public async Task<List<TaskSession>> GetAllTaskSessionsAsync() =>
            await _taskSessionsCollection.Find(_ => true).ToListAsync();

        public async Task<TaskSession> GetTaskSessionByIdAsync(string id) =>
            await _taskSessionsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<TaskSession> CreateTaskSessionAsync(TaskSession taskSession)
        {
            if (string.IsNullOrEmpty(taskSession.Id))
                taskSession.Id = ObjectId.GenerateNewId().ToString();

            taskSession.CreatedAt = DateTime.UtcNow;
            taskSession.UpdatedAt = DateTime.UtcNow;

            await _taskSessionsCollection.InsertOneAsync(taskSession);
            return taskSession;
        }

        public async Task<TaskSession> UpdateTaskSessionAsync(string id, TaskSession updatedSession)
        {
            updatedSession.Id = id;
            updatedSession.UpdatedAt = DateTime.UtcNow;

            await _taskSessionsCollection.ReplaceOneAsync(x => x.Id == id, updatedSession);
            return updatedSession;
        }

        public async Task DeleteTaskSessionAsync(string id) =>
            await _taskSessionsCollection.DeleteOneAsync(x => x.Id == id);

        // Task Program methods
        public async Task<List<TaskProgram>> GetAllTaskProgramsAsync() =>
            await _taskProgramsCollection.Find(_ => true).ToListAsync();

        public async Task<List<TaskProgram>> GetTaskProgramsBySessionIdAsync(string sessionId) =>
            await _taskProgramsCollection.Find(x => x.SessionId == sessionId).ToListAsync();

        public async Task<TaskProgram> GetTaskProgramByIdAsync(string id) =>
            await _taskProgramsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<TaskProgram> CreateTaskProgramAsync(TaskProgram program)
        {
            if (string.IsNullOrEmpty(program.Id))
                program.Id = ObjectId.GenerateNewId().ToString();

            program.CreatedAt = DateTime.UtcNow;
            program.UpdatedAt = DateTime.UtcNow;

            await _taskProgramsCollection.InsertOneAsync(program);
            return program;
        }

        public async Task<TaskProgram> UpdateTaskProgramAsync(string id, TaskProgram updatedProgram)
        {
            updatedProgram.Id = id;
            updatedProgram.UpdatedAt = DateTime.UtcNow;

            await _taskProgramsCollection.ReplaceOneAsync(x => x.Id == id, updatedProgram);
            return updatedProgram;
        }

        public async Task DeleteTaskProgramAsync(string id) =>
            await _taskProgramsCollection.DeleteOneAsync(x => x.Id == id);

        // Task Item methods
        public async Task<List<TaskItem>> GetAllTaskItemsAsync() =>
            await _taskItemsCollection.Find(_ => true).ToListAsync();

        public async Task<List<TaskItem>> GetTaskItemsByProgramIdAsync(string programId) =>
            await _taskItemsCollection.Find(x => x.ProgramId == programId).ToListAsync();

        public async Task<TaskItem> GetTaskItemByIdAsync(string id) =>
            await _taskItemsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<TaskItem> CreateTaskItemAsync(TaskItem item)
        {
            if (string.IsNullOrEmpty(item.Id))
                item.Id = ObjectId.GenerateNewId().ToString();

            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            await _taskItemsCollection.InsertOneAsync(item);
            return item;
        }

        public async Task<TaskItem> UpdateTaskItemAsync(string id, TaskItem updatedItem)
        {
            updatedItem.Id = id;
            updatedItem.UpdatedAt = DateTime.UtcNow;

            await _taskItemsCollection.ReplaceOneAsync(x => x.Id == id, updatedItem);
            return updatedItem;
        }

        public async Task<TaskItem> CompleteTaskItemAsync(string id)
        {
            var item = await GetTaskItemByIdAsync(id);
            if (item != null)
            {
                item.Status = TaskItemStatus.Completed;
                item.UpdatedAt = DateTime.UtcNow;
                await _taskItemsCollection.ReplaceOneAsync(x => x.Id == id, item);
            }
            return item;
        }

        public async Task DeleteTaskItemAsync(string id) =>
            await _taskItemsCollection.DeleteOneAsync(x => x.Id == id);
    }
}