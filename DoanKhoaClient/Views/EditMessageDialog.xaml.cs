using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class EditMessageDialog : Window
    {
        public string EditedContent { get; private set; }

        public EditMessageDialog(string originalContent)
        {
            InitializeComponent();
            MessageTextBox.Text = originalContent;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                EditedContent = MessageTextBox.Text;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Tin nhắn không được để trống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}