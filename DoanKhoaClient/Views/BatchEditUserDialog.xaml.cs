using DoanKhoaClient.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace DoanKhoaClient.Views
{
    public partial class BatchEditUserDialog : Window, INotifyPropertyChanged
    {
        private BatchEditOptions _editOptions;
        private string _newPassword = string.Empty;
        private string _adminCode = string.Empty;

        public BatchEditOptions EditOptions
        {
            get => _editOptions;
            set
            {
                _editOptions = value;
                OnPropertyChanged();
            }
        }

        public string NewPassword => _newPassword;
        public string AdminCode => _adminCode;

        public BatchEditUserDialog()
        {
            InitializeComponent();
            EditOptions = new BatchEditOptions();
            DataContext = this;
        }

        private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var roleTag = selectedItem.Tag.ToString();
                EditOptions.Role = (UserRole)Enum.Parse(typeof(UserRole), roleTag);

                // Hiển thị Admin Code section nếu chọn Admin
                AdminCodeSection.Visibility = (roleTag == "Admin") ? Visibility.Visible : Visibility.Collapsed;

                // Reset admin code nếu không phải Admin
                if (roleTag != "Admin")
                {
                    AdminCodeBox.Password = string.Empty;
                    _adminCode = string.Empty;
                }
            }
        }

        private void AdminCodeBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _adminCode = passwordBox.Password;
            }
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _newPassword = passwordBox.Password;
            }
        }

        private bool ValidateInput()
        {
            // Kiểm tra có ít nhất một trường được chọn
            if (!EditOptions.UpdateRole &&
                !EditOptions.UpdatePosition &&
                !EditOptions.UpdatePassword &&
                !EditOptions.UpdateStatus)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một trường để cập nhật.",
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validate Role selection
            if (EditOptions.UpdateRole)
            {
                if (RoleBox.SelectedItem == null)
                {
                    MessageBox.Show("Vui lòng chọn vai trò.",
                        "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                    RoleBox.Focus();
                    return false;
                }

                // Validate Admin Code if selecting Admin role
                var selectedRoleItem = RoleBox.SelectedItem as ComboBoxItem;
                if (selectedRoleItem?.Tag.ToString() == "Admin" && string.IsNullOrWhiteSpace(_adminCode))
                {
                    MessageBox.Show("Vui lòng nhập mã xác thực Admin.",
                        "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                    AdminCodeBox.Focus();
                    return false;
                }
            }

            // Validate Position selection
            if (EditOptions.UpdatePosition && PositionBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn chức vụ.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                PositionBox.Focus();
                return false;
            }

            // Validate Password
            if (EditOptions.UpdatePassword)
            {
                if (string.IsNullOrWhiteSpace(_newPassword))
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu mới.",
                        "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                    NewPasswordBox.Focus();
                    return false;
                }

                if (_newPassword.Length < 6)
                {
                    MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự.",
                        "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                    NewPasswordBox.Focus();
                    return false;
                }
            }

            // Validate Status selection
            if (EditOptions.UpdateStatus && StatusBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn trạng thái xác thực email.",
                    "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusBox.Focus();
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (!ValidateInput())
                    return;

                // Cập nhật các giá trị từ UI vào EditOptions trước khi validate
                if (EditOptions.UpdateRole && RoleBox.SelectedItem is ComboBoxItem roleItem)
                {
                    EditOptions.Role = (UserRole)Enum.Parse(typeof(UserRole), roleItem.Tag.ToString());
                }

                if (EditOptions.UpdatePosition && PositionBox.SelectedItem is ComboBoxItem positionItem)
                {
                    EditOptions.Position = (Position)Enum.Parse(typeof(Position), positionItem.Tag.ToString());
                }

                if (EditOptions.UpdateStatus && StatusBox.SelectedItem is ComboBoxItem statusItem)
                {
                    EditOptions.EmailVerified = bool.Parse(statusItem.Tag.ToString());
                }

                // Confirm action
                var fieldsToUpdate = new System.Collections.Generic.List<string>();
                if (EditOptions.UpdateRole) fieldsToUpdate.Add("Vai trò");
                if (EditOptions.UpdatePosition) fieldsToUpdate.Add("Chức vụ");
                if (EditOptions.UpdatePassword) fieldsToUpdate.Add("Mật khẩu");
                if (EditOptions.UpdateStatus) fieldsToUpdate.Add("Trạng thái xác thực");

                var message = $"Bạn có chắc chắn muốn cập nhật các trường sau cho tất cả thành viên được chọn?\n\n" +
                             $"• {string.Join("\n• ", fieldsToUpdate)}\n\n" +
                             $"Thao tác này không thể hoàn tác!";

                var result = MessageBox.Show(message, "Xác nhận cập nhật hàng loạt",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Lớp chứa các tùy chọn chỉnh sửa hàng loạt - ĐÃ XÓA DISPLAYNAME VÀ EMAIL
    public class BatchEditOptions : INotifyPropertyChanged
    {
        // Role
        private bool _updateRole;
        private UserRole? _role;

        // Position
        private bool _updatePosition;
        private Position? _position;

        // Password
        private bool _updatePassword;

        // Status
        private bool _updateStatus;
        private bool? _emailVerified;

        public bool UpdateRole
        {
            get => _updateRole;
            set
            {
                _updateRole = value;
                OnPropertyChanged();
            }
        }

        public UserRole? Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        public bool UpdatePosition
        {
            get => _updatePosition;
            set
            {
                _updatePosition = value;
                OnPropertyChanged();
            }
        }

        public Position? Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
            }
        }

        public bool UpdatePassword
        {
            get => _updatePassword;
            set
            {
                _updatePassword = value;
                OnPropertyChanged();
            }
        }

        public bool UpdateStatus
        {
            get => _updateStatus;
            set
            {
                _updateStatus = value;
                OnPropertyChanged();
            }
        }

        public bool? EmailVerified
        {
            get => _emailVerified;
            set
            {
                _emailVerified = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}