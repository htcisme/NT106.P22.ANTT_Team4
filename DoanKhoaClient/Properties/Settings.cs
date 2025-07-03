namespace DoanKhoaClient.Properties
{
    public class Settings
    {
        private static Settings _default;
        public static Settings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new Settings();
                }
                return _default;
            }
        }

        // Các cài đặt lọc spam
        public bool AutoFilterSpam { get; set; } = true;
        public bool NotifyOnSpamDetection { get; set; } = true;
        public int SpamFilterLevel { get; set; } = 5;

        public void Save()
        {
            // Trong phiên bản đầy đủ, bạn sẽ lưu vào file cấu hình
            // Đây là cài đặt tạm thời chỉ lưu trong bộ nhớ
        }
    }
}