using System;
using System.Windows;
using System.Windows.Controls;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class EditUserDialog : Window
    {
        private User _originalUser;
        public User User { get; private set; }
        public string AdminCode { get; private set; } // Thêm thuộc tính cho Admin Code

        private bool _isChangingToAdmin = false; // Biến để theo dõi việc thay đổi lên Admin

        public EditUserDialog(User user)
        {
            InitializeComponent();

            _originalUser = user;
            User = new User
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = user.Role,
                Position = user.Position,
                // Sao chép các trường khác nếu cần
            };

            // Thiết lập các giá trị cho controls
            UsernameBox.Text = User.Username;
            DisplayNameBox.Text = User.DisplayName;
            EmailBox.Text = User.Email;

            // Chọn Role trong ComboBox
            foreach (ComboBoxItem item in RoleBox.Items)
            {
                if (item.Tag.ToString() == User.Role.ToString())
                {
                    RoleBox.SelectedItem = item;
                    break;
                }
            }

            // Chọn Position trong ComboBox
            foreach (ComboBoxItem item in PositionBox.Items)
            {
                if (item.Tag.ToString() == User.Position.ToString())
                {
                    PositionBox.SelectedItem = item;
                    break;
                }
            }

            // Thêm event handler cho RoleBox
            RoleBox.SelectionChanged += RoleBox_SelectionChanged;

            // Kiểm tra và hiển thị Admin Code panel nếu là Admin
            UpdateAdminCodePanelVisibility();
        }

        private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Kiểm tra xem có đang thay đổi từ User lên Admin không
            var selectedRole = (RoleBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            _isChangingToAdmin = (selectedRole == "Admin" && _originalUser.Role != UserRole.Admin);

            // Cập nhật hiển thị của Admin Code panel
            UpdateAdminCodePanelVisibility();
        }

        private void UpdateAdminCodePanelVisibility()
        {
            // Hiển thị Admin Code panel nếu đang thay đổi lên Admin
            AdminCodePanel.Visibility = _isChangingToAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Xóa nội dung Admin Code box nếu không còn hiển thị
            if (AdminCodePanel.Visibility == Visibility.Collapsed)
            {
                AdminCodeBox.Password = string.Empty;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cập nhật thông tin người dùng từ UI
                User.DisplayName = DisplayNameBox.Text.Trim();
                User.Email = EmailBox.Text.Trim();

                // Lấy Role từ ComboBox
                var roleItem = RoleBox.SelectedItem as ComboBoxItem;
                User.Role = (UserRole)Enum.Parse(typeof(UserRole), roleItem?.Tag?.ToString() ?? "User");

                // Lấy Position từ ComboBox
                var positionItem = PositionBox.SelectedItem as ComboBoxItem;
                User.Position = (Position)Enum.Parse(typeof(Position), positionItem?.Tag?.ToString() ?? "None");

                // Kiểm tra và lấy Admin Code nếu đang thay đổi lên Admin
                if (_isChangingToAdmin)
                {
                    AdminCode = AdminCodeBox.Password;

                    if (string.IsNullOrWhiteSpace(AdminCode))
                    {
                        MessageBox.Show("Vui lòng nhập mã xác thực Admin.",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Validation cơ bản
                if (string.IsNullOrWhiteSpace(User.DisplayName))
                {
                    MessageBox.Show("Vui lòng nhập họ tên.",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(User.Email))
                {
                    MessageBox.Show("Vui lòng nhập email.",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Đóng dialog với kết quả thành công
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}",
                    "Lỗi khi lưu thông tin", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Đóng dialog với kết quả hủy
            DialogResult = false;
        }
    }
}