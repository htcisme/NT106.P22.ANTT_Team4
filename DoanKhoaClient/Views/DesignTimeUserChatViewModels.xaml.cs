// Tạo file mới: DesignTimeUserChatViewModel.cs
namespace DoanKhoaClient.ViewModels
{
    public class DesignTimeUserChatViewModel : UserChatViewModel
    {
        public DesignTimeUserChatViewModel()
        {
            // Bỏ qua tất cả các tác vụ mạng, chỉ tạo dữ liệu mẫu
            // KHÔNG gọi base constructor

            // Tạo dữ liệu giả cho designer
            CurrentUser = new Models.User
            {
                Id = "design-user",
                Username = "DesignUser",
                DisplayName = "Design Time User"
            };

            // Thêm một số cuộc trò chuyện mẫu
            Conversations = new System.Collections.ObjectModel.ObservableCollection<Models.Conversation>
            {
                new Models.Conversation {
                    Id = "conv1",
                    Title = "Cuộc hội thoại mẫu 1",
                    LastActivity = DateTime.Now
                },
                new Models.Conversation {
                    Id = "conv2",
                    Title = "Cuộc hội thoại mẫu 2",
                    LastActivity = DateTime.Now.AddDays(-1)
                }
            };
        }
    }
}