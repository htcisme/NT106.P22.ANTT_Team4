using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class EmailInputDialog : Window
    {
        public string EnteredEmail { get; private set; }
        public string EnteredName { get; private set; }

        public EmailInputDialog(string taskTitle, string assigneeName)
        {
            InitializeComponent();

            // Set task title
            TaskTitleLabel.Content = $"Công việc: {taskTitle}";

            // Pre-fill name if available
            if (!string.IsNullOrEmpty(assigneeName))
            {
                NameTextBox.Text = assigneeName;
            }

            // Focus vào email textbox
            EmailTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ email!", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return;
            }

            if (!IsValidEmail(EmailTextBox.Text.Trim()))
            {
                MessageBox.Show("Địa chỉ email không hợp lệ!\n\nVí dụ email hợp lệ: user@domain.com", "Email không hợp lệ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                EmailTextBox.SelectAll();
                return;
            }

            // Validate name
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                var result = MessageBox.Show("Bạn chưa nhập tên người nhận.\n\nBạn có muốn tiếp tục với tên mặc định 'Người nhận'?",
                    "Thiếu tên người nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    NameTextBox.Focus();
                    return;
                }
            }

            EnteredEmail = EmailTextBox.Text.Trim();
            EnteredName = string.IsNullOrWhiteSpace(NameTextBox.Text) ? "Người nhận" : NameTextBox.Text.Trim();

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // THÊM: Handle Enter key để submit
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OkButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(this, new RoutedEventArgs());
            }

            base.OnKeyDown(e);
        }
    }
}