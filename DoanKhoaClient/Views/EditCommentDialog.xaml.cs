using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class EditCommentDialog : Window
    {
        public string NewContent { get; private set; }
        private string _originalContent;

        public EditCommentDialog(string originalContent)
        {
            InitializeComponent();
            _originalContent = originalContent ?? string.Empty;
            ContentTextBox.Text = _originalContent;
            ContentTextBox.Focus();
            ContentTextBox.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var content = ContentTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("Vui lòng nhập nội dung bình luận.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ContentTextBox.Focus();
                return;
            }

            if (content.Length > 1000)
            {
                MessageBox.Show("Nội dung bình luận không được vượt quá 1000 ký tự.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ContentTextBox.Focus();
                return;
            }

            NewContent = content;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                DialogResult = false;
            }
        }
    }
}