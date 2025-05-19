using System;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using DoanKhoaClient.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DoanKhoaClient.Views
{
    public partial class AddUserDialog : Window, INotifyPropertyChanged
    {
        public User User { get; private set; }
        public class AddUserResult
        {
            public User User { get; set; }
            public string Password { get; set; }
            public string AdminCode { get; set; }
        }

        public AddUserResult Result { get; private set; }

        // Private fields
        private string _username;
        private string _displayName;
        private string _email;
        private string _securePassword = string.Empty;
        private string _secureAdminCode = string.Empty;
        private bool _isAdmin;
        private string _usernameError;
        private string _displayNameError;
        private string _emailError;
        private string _passwordError;
        private string _adminCodeError;
        private bool _showAdminCode;
        private Visibility _adminCodeVisibility = Visibility.Collapsed;

        // Properties with change notification
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                    ValidateFields();
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                    ValidateFields();
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                    ValidateFields();
                }
            }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (_isAdmin != value)
                {
                    _isAdmin = value;
                    OnPropertyChanged();
                    ShowAdminCode = value;

                    // Reset admin code error when unchecking the box
                    if (!value)
                        AdminCodeError = string.Empty;

                    ValidateFields();
                }
            }
        }

        public string UsernameError
        {
            get => _usernameError;
            set
            {
                if (_usernameError != value)
                {
                    _usernameError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayNameError
        {
            get => _displayNameError;
            set
            {
                if (_displayNameError != value)
                {
                    _displayNameError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EmailError
        {
            get => _emailError;
            set
            {
                if (_emailError != value)
                {
                    _emailError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PasswordError
        {
            get => _passwordError;
            set
            {
                if (_passwordError != value)
                {
                    _passwordError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AdminCodeError
        {
            get => _adminCodeError;
            set
            {
                if (_adminCodeError != value)
                {
                    _adminCodeError = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowAdminCode
        {
            get => _showAdminCode;
            set
            {
                if (_showAdminCode != value)
                {
                    _showAdminCode = value;
                    AdminCodeVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility AdminCodeVisibility
        {
            get => _adminCodeVisibility;
            set
            {
                if (_adminCodeVisibility != value)
                {
                    _adminCodeVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        // Constructor
        public AddUserDialog()
        {
            InitializeComponent();
            this.DataContext = this;

            // Set default values to avoid null reference exceptions
            Username = string.Empty;
            DisplayName = string.Empty;
            Email = string.Empty;
        }

        private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRoleItem = RoleBox.SelectedItem as ComboBoxItem;
            var roleTag = selectedRoleItem?.Tag?.ToString() ?? "User";
            IsAdmin = (roleTag == "Admin");
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _securePassword = passwordBox.Password;

                // Validate the password
                if (string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    PasswordError = "Vui lòng nhập mật khẩu";
                }
                else if (passwordBox.Password.Length < 6)
                {
                    PasswordError = "Mật khẩu phải có ít nhất 6 ký tự";
                }
                else
                {
                    PasswordError = string.Empty;
                }

                ValidateFields();
            }
        }

        private void AdminCodeBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _secureAdminCode = passwordBox.Password;

                // Validate admin code
                if (IsAdmin && string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    AdminCodeError = "Vui lòng nhập mã xác thực Admin";
                }
                else
                {
                    AdminCodeError = string.Empty;
                }

                ValidateFields();
            }
        }

        private void ResetErrors()
        {
            UsernameError = string.Empty;
            DisplayNameError = string.Empty;
            EmailError = string.Empty;
            // Giữ lại PasswordError và AdminCodeError vì chúng được cập nhật riêng
        }

        private void ValidateFields()
        {
            // Reset username, display name and email errors
            UsernameError = string.Empty;
            DisplayNameError = string.Empty;
            EmailError = string.Empty;

            // Validate individual fields
            if (string.IsNullOrWhiteSpace(Username))
                UsernameError = "Vui lòng nhập tên đăng nhập";

            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayNameError = "Vui lòng nhập họ tên";

            if (string.IsNullOrWhiteSpace(Email))
                EmailError = "Vui lòng nhập email";
            else if (!IsValidEmail(Email))
                EmailError = "Email không hợp lệ";

            // Password và AdminCode được kiểm tra trong event handlers
        }

        private bool HasValidationErrors()
        {
            return !string.IsNullOrEmpty(UsernameError) ||
                   !string.IsNullOrEmpty(DisplayNameError) ||
                   !string.IsNullOrEmpty(EmailError) ||
                   !string.IsNullOrEmpty(PasswordError) ||
                   (IsAdmin && !string.IsNullOrEmpty(AdminCodeError));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Update values from UI controls
            Username = UsernameBox.Text.Trim();
            DisplayName = DisplayNameBox.Text.Trim();
            Email = EmailBox.Text.Trim();

            // Kiểm tra mật khẩu
            if (string.IsNullOrWhiteSpace(_securePassword))
            {
                PasswordError = "Vui lòng nhập mật khẩu";
            }
            else if (_securePassword.Length < 6)
            {
                PasswordError = "Mật khẩu phải có ít nhất 6 ký tự";
            }

            var selectedRoleItem = RoleBox.SelectedItem as ComboBoxItem;
            var roleTag = selectedRoleItem?.Tag?.ToString() ?? "User";
            IsAdmin = (roleTag == "Admin");

            if (IsAdmin && string.IsNullOrWhiteSpace(_secureAdminCode))
            {
                AdminCodeError = "Vui lòng nhập mã xác thực Admin";
            }

            // Validate all fields
            if (HasValidationErrors())
            {
                // Show validation errors in a message box
                string errorMessage = "Vui lòng kiểm tra lại thông tin:";

                if (!string.IsNullOrEmpty(UsernameError))
                    errorMessage += $"\n- {UsernameError}";

                if (!string.IsNullOrEmpty(DisplayNameError))
                    errorMessage += $"\n- {DisplayNameError}";

                if (!string.IsNullOrEmpty(EmailError))
                    errorMessage += $"\n- {EmailError}";

                if (!string.IsNullOrEmpty(PasswordError))
                    errorMessage += $"\n- {PasswordError}";

                if (IsAdmin && !string.IsNullOrEmpty(AdminCodeError))
                    errorMessage += $"\n- {AdminCodeError}";

                MessageBox.Show(errorMessage, "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create User object with the form data
                User = new User
                {
                    Username = Username,
                    DisplayName = DisplayName,
                    Email = Email,
                    Role = (UserRole)Enum.Parse(typeof(UserRole), roleTag),
                    Position = (Position)Enum.Parse(typeof(Position), ((ComboBoxItem)PositionBox.SelectedItem).Tag.ToString())
                };

                // Tạo Result với User và thông tin mật khẩu
                Result = new AddUserResult
                {
                    User = User,
                    Password = _securePassword,
                    AdminCode = IsAdmin ? _secureAdminCode : string.Empty
                };

                // Close dialog with success
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi khi tạo người dùng", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use same email validation regex as in RegisterViewModel
                var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}