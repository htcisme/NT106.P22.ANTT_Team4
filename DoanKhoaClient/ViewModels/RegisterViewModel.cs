using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
namespace DoanKhoaClient.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly AuthService _authService;
        private string _fullName;
        private string _email;
        private string _username;
        private string _password;
        private string _confirmPassword;
        private string _passwordError;
        private string _emailError;
        private string _usernameError;
        private string _fullNameError;
        private string _confirmPasswordError;
        private string _errorMessage;
        private bool _isLoading;
        private bool _enableTwoFactorAuth;
        private Visibility _fullNamePlaceholderVisibility = Visibility.Visible;
        private Visibility _emailPlaceholderVisibility = Visibility.Visible;
        private Visibility _usernamePlaceholderVisibility = Visibility.Visible;
        private Visibility _passwordPlaceholderVisibility = Visibility.Visible;
        private Visibility _confirmPasswordPlaceholderVisibility = Visibility.Visible;
        #endregion

        #region Properties
        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged();
                    FullNamePlaceholderVisibility = string.IsNullOrWhiteSpace(value)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    ValidateCanRegister();
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
                    EmailPlaceholderVisibility = string.IsNullOrWhiteSpace(value)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    ValidateCanRegister();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                    UsernamePlaceholderVisibility = string.IsNullOrWhiteSpace(value)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    ValidateCanRegister();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                    ValidateCanRegister();
                }
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (_confirmPassword != value)
                {
                    _confirmPassword = value;
                    OnPropertyChanged();
                    ValidateCanRegister();
                }
            }
        }

        public bool EnableTwoFactorAuth
        {
            get => _enableTwoFactorAuth;
            set
            {
                if (_enableTwoFactorAuth != value)
                {
                    _enableTwoFactorAuth = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public Visibility FullNamePlaceholderVisibility
        {
            get => _fullNamePlaceholderVisibility;
            set
            {
                if (_fullNamePlaceholderVisibility != value)
                {
                    _fullNamePlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility EmailPlaceholderVisibility
        {
            get => _emailPlaceholderVisibility;
            set
            {
                if (_emailPlaceholderVisibility != value)
                {
                    _emailPlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility UsernamePlaceholderVisibility
        {
            get => _usernamePlaceholderVisibility;
            set
            {
                if (_usernamePlaceholderVisibility != value)
                {
                    _usernamePlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility PasswordPlaceholderVisibility
        {
            get => _passwordPlaceholderVisibility;
            set
            {
                if (_passwordPlaceholderVisibility != value)
                {
                    _passwordPlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility ConfirmPasswordPlaceholderVisibility
        {
            get => _confirmPasswordPlaceholderVisibility;
            set
            {
                if (_confirmPasswordPlaceholderVisibility != value)
                {
                    _confirmPasswordPlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion
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

        public string FullNameError
        {
            get => _fullNameError;
            set
            {
                if (_fullNameError != value)
                {
                    _fullNameError = value;
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

        public string ConfirmPasswordError
        {
            get => _confirmPasswordError;
            set
            {
                if (_confirmPasswordError != value)
                {
                    _confirmPasswordError = value;
                    OnPropertyChanged();
                }
            }
        }
        #region Commands
        public ICommand RegisterCommand { get; private set; }
        public ICommand NavigateToLoginCommand { get; private set; }
        public ICommand PasswordChangedCommand { get; private set; }
        public ICommand ConfirmPasswordChangedCommand { get; private set; }
        public ICommand UsernameChangedCommand { get; private set; }
        #endregion

        #region Constructor
        public RegisterViewModel()
        {
            _authService = new AuthService();

            // Khởi tạo commands
            RegisterCommand = new RelayCommand(ExecuteRegister, CanExecuteRegister);
            NavigateToLoginCommand = new RelayCommand(ExecuteNavigateToLogin);
            PasswordChangedCommand = new RelayCommand(ExecutePasswordChanged);
            ConfirmPasswordChangedCommand = new RelayCommand(ExecuteConfirmPasswordChanged);
            UsernameChangedCommand = new RelayCommand(ExecuteUsernameChanged);
        }
        #endregion

        #region Command Methods
        private bool CanExecuteRegister(object parameter)
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   Password == ConfirmPassword;
        }
        private void ResetErrors()
        {
            UsernameError = string.Empty;
            FullNameError = string.Empty;
            EmailError = string.Empty;
            PasswordError = string.Empty;
            ConfirmPasswordError = string.Empty;
            ErrorMessage = string.Empty;
        }
        private void ValidateCanRegister()
        {
            // Reset tất cả các thông báo lỗi
            ResetErrors();

            // Kiểm tra từng trường và đặt lỗi
            if (string.IsNullOrWhiteSpace(Username))
                UsernameError = "Vui lòng nhập tên đăng nhập";

            if (string.IsNullOrWhiteSpace(FullName))
                FullNameError = "Vui lòng nhập họ tên";

            if (string.IsNullOrWhiteSpace(Email))
                EmailError = "Vui lòng nhập email";
            else if (!IsValidEmail(Email))
                EmailError = "Email không hợp lệ";

            if (string.IsNullOrWhiteSpace(Password))
                PasswordError = "Vui lòng nhập mật khẩu";
            else if (Password.Length < 6)
                PasswordError = "Mật khẩu phải có ít nhất 6 ký tự";

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
                ConfirmPasswordError = "Vui lòng xác nhận mật khẩu";
            else if (Password != ConfirmPassword)
                ConfirmPasswordError = "Mật khẩu xác nhận không khớp";

            (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }


        private async void ExecuteRegister(object parameter)
        {
            try
            {
                IsLoading = true;
                ResetErrors(); // Xóa tất cả lỗi trước khi bắt đầu

                bool hasError = false;

                // Kiểm tra từng trường
                if (string.IsNullOrWhiteSpace(Username))
                {
                    UsernameError = "Vui lòng nhập tên đăng nhập";
                    hasError = true;
                }

                if (string.IsNullOrWhiteSpace(FullName))
                {
                    FullNameError = "Vui lòng nhập họ tên";
                    hasError = true;
                }

                if (string.IsNullOrWhiteSpace(Email))
                {
                    EmailError = "Vui lòng nhập email";
                    hasError = true;
                }
                else if (!IsValidEmail(Email))
                {
                    EmailError = "Email không hợp lệ";
                    hasError = true;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    PasswordError = "Vui lòng nhập mật khẩu";
                    hasError = true;
                }
                else if (Password.Length < 6)
                {
                    PasswordError = "Mật khẩu phải có ít nhất 6 ký tự";
                    hasError = true;
                }

                if (string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    ConfirmPasswordError = "Vui lòng xác nhận mật khẩu";
                    hasError = true;
                }
                else if (Password != ConfirmPassword)
                {
                    ConfirmPasswordError = "Mật khẩu xác nhận không khớp";
                    hasError = true;
                }

                if (hasError)
                {
                    ErrorMessage = "Vui lòng kiểm tra lại thông tin đăng ký.";
                    return;
                }

                // Tạo request để đăng ký
                var request = new RegisterRequest
                {
                    Username = Username,
                    DisplayName = FullName,
                    Email = Email,
                    Password = Password,
                    EnableTwoFactorAuth = EnableTwoFactorAuth
                };

                System.Diagnostics.Debug.WriteLine($"Gửi request đăng ký: Username={Username}, Email={Email}");

                // Gửi request đăng ký
                var response = await _authService.RegisterAsync(request);

                System.Diagnostics.Debug.WriteLine($"Nhận phản hồi từ server: {response?.Message ?? "null"}");


                if (response != null && !string.IsNullOrEmpty(response.Id))
                {
                    // Đơn giản hóa, không cần sử dụng Dispatcher.Invoke vì đang ở UI thread
                    MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    System.Diagnostics.Debug.WriteLine("Đăng ký thành công, chuyển đến trang đăng nhập...");

                    // Sử dụng phương thức đơn giản hơn
                    OpenLoginWindowAndCloseRegister();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng ký: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Exception khi đăng ký: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenLoginWindowAndCloseRegister()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Đang chuyển đến trang đăng nhập...");

                // Tạo trang đăng nhập mới
                var loginWindow = new LoginView();
                loginWindow.Show();

                // Tìm và đóng trang đăng ký hiện tại
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is RegisterView)
                    {
                        window.Close();
                        break;
                    }
                }

                System.Diagnostics.Debug.WriteLine("Đã chuyển đến trang đăng nhập thành công!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi chuyển trang: {ex.Message}");
                MessageBox.Show($"Không thể chuyển đến trang đăng nhập: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteNavigateToLogin(object parameter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Đang chuyển về trang đăng nhập...");

                // Tạo và hiển thị cửa sổ đăng nhập
                var loginWindow = new LoginView();
                loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Hiển thị cửa sổ đăng nhập
                loginWindow.Show();
                System.Diagnostics.Debug.WriteLine("Đã hiển thị cửa sổ đăng nhập");

                // Đặt cửa sổ đăng nhập là MainWindow mới của ứng dụng
                Application.Current.MainWindow = loginWindow;

                // Tìm và đóng cửa sổ đăng ký hiện tại
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != loginWindow && window is RegisterView)
                    {
                        System.Diagnostics.Debug.WriteLine("Đóng cửa sổ đăng ký");
                        window.Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Lỗi chuyển đến trang đăng nhập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecutePasswordChanged(object parameter)
        {
            if (parameter is PasswordBox passwordBox)
            {
                Password = passwordBox.Password;
                PasswordPlaceholderVisibility = string.IsNullOrWhiteSpace(passwordBox.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void ExecuteConfirmPasswordChanged(object parameter)
        {
            if (parameter is PasswordBox passwordBox)
            {
                ConfirmPassword = passwordBox.Password;
                ConfirmPasswordPlaceholderVisibility = string.IsNullOrWhiteSpace(passwordBox.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void ExecuteUsernameChanged(object parameter)
        {
            if (parameter is TextBox textBox)
            {
                Username = textBox.Text;
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region RelayCommand Implementation
        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null || _canExecute(parameter);
            }

            public void Execute(object parameter)
            {
                _execute(parameter);
            }

            public event EventHandler CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Kiểm tra định dạng email bằng regex
                var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}