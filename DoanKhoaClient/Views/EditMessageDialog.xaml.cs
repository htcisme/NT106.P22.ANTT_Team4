using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class EditMessageDialog : Window
    {
        public string EditedContent { get; private set; }

        public EditMessageDialog(string originalContent)
        {
            InitializeComponent();

            // Thiết lập nội dung ban đầu
            MessageTextBox.Text = originalContent ?? string.Empty;
            EditedContent = originalContent ?? string.Empty;

            // Focus vào TextBox và select all text
            MessageTextBox.Focus();
            MessageTextBox.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            EditedContent = MessageTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(EditedContent))
            {
                MessageBox.Show("Nội dung tin nhắn không được để trống!", "Cảnh báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageTextBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MessageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Cho phép Enter để xuống dòng, Ctrl+Enter để lưu
            if (e.Key == System.Windows.Input.Key.Enter &&
                System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                SaveButton_Click(sender, null);
                e.Handled = true;
            }
            // Escape để hủy
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(sender, null);
                e.Handled = true;
            }
        }
    }
}