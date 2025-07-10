namespace DoanKhoaServer.Settings
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string UsersCollectionName { get; set; } = string.Empty;
        public string MessagesCollectionName { get; set; } = string.Empty;
        public string ConversationsCollectionName { get; set; } = string.Empty;
        public string AttachmentsCollectionName { get; set; } = string.Empty;
        public string ActivitiesCollectionName { get; set; } = string.Empty;
        public string TaskSessionsCollectionName { get; set; }
        public string TaskProgramsCollectionName { get; set; }
        public string TaskItemsCollectionName { get; set; }
        public string UserActivityStatusesCollectionName { get; set; }
        public string CommentsCollectionName { get; set; } 
        public string UserCommentStatusesCollectionName { get; set; } 
    }
}