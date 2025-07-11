using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class EditUserDialog : Window
    {
        private User _originalUser;
        public User User { get; private set; }
        public string AdminCode { get; private set; }

        private bool _isChangingToAdmin = false;

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
                AvatarUrl = user.AvatarUrl,
                LastSeen = user.LastSeen,
                ActivitiesCount = user.ActivitiesCount,
                EmailVerified = user.EmailVerified,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Conversations = user.Conversations,
            };

            InitializeControls();
        }

        private void InitializeControls()
        {
            // Thiết lập các giá trị cho controls
            UsernameBox.Text = User.Username;
            UsernameBox.IsReadOnly = true; // Username không được phép sửa
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

            // Kiểm tra và hiển thị Admin Code panel nếu cần
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
                AdminCode = string.Empty;
            }
        }

        private bool ValidateInput()
        {
            // Validate Display Name
            if (string.IsNullOrWhiteSpace(DisplayNameBox.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                DisplayNameBox.Focus();
                return false;
            }

            // Validate Email
            if (string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                MessageBox.Show("Vui lòng nhập email.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                EmailBox.Focus();
                return false;
            }

            if (!IsValidEmail(EmailBox.Text.Trim()))
            {
                MessageBox.Show("Email không hợp lệ. Vui lòng nhập đúng định dạng email.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                EmailBox.Focus();
                return false;
            }

            // Validate Admin Code if promoting to Admin
            if (_isChangingToAdmin && string.IsNullOrWhiteSpace(AdminCodeBox.Password))
            {
                MessageBox.Show("Vui lòng nhập mã xác thực Admin.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                AdminCodeBox.Focus();
                return false;
            }

            // Validate Role selection
            if (RoleBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn vai trò.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                RoleBox.Focus();
                return false;
            }

            // Validate Position selection
            if (PositionBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn chức vụ.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                PositionBox.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input trước khi lưu
                if (!ValidateInput())
                    return;

                // TẠO MỘT OBJECT CHỈ CHỨA CÁC TRƯỜNG CẦN CẬP NHẬT
                // Không gửi toàn bộ User object để tránh lỗi validation
                var updateRequest = new
                {
                    DisplayName = DisplayNameBox.Text.Trim(),
                    Email = EmailBox.Text.Trim(),
                    Role = (UserRole)Enum.Parse(typeof(UserRole), ((ComboBoxItem)RoleBox.SelectedItem).Tag.ToString()),
                    Position = (Position)Enum.Parse(typeof(Position), ((ComboBoxItem)PositionBox.SelectedItem).Tag.ToString()),
                    AvatarUrl = User.AvatarUrl ?? string.Empty,
                    // Không cần gửi PasswordHash nếu không thay đổi

                };

                // Cập nhật User object để trả về
                User.DisplayName = updateRequest.DisplayName;
                User.Email = updateRequest.Email;
                User.Role = updateRequest.Role;
                User.Position = updateRequest.Position;
                User.AvatarUrl = updateRequest.AvatarUrl;


                // Lấy Admin Code nếu đang thay đổi lên Admin
                if (_isChangingToAdmin)
                {
                    AdminCode = AdminCodeBox.Password;
                }

                // Đóng dialog với kết quả thành công
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu thông tin: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Đóng dialog với kết quả hủy
            DialogResult = false;
        }
    }
}