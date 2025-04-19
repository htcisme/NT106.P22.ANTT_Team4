using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DoanKhoaClient.Views;

namespace DoanKhoaClient.ViewModels
{
    public class LightRegisterViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _fullName;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _errorMessage;
        private Visibility _fullNamePlaceholderVisibility = Visibility.Visible;
        private Visibility _emailPlaceholderVisibility = Visibility.Visible;
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

        #region Commands
        public ICommand RegisterCommand { get; private set; }
        public ICommand NavigateToLoginCommand { get; private set; }
        public ICommand PasswordChangedCommand { get; private set; }
        public ICommand ConfirmPasswordChangedCommand { get; private set; }
        #endregion

        #region Constructor
        public LightRegisterViewModel()
        {
            // Khởi tạo commands
            RegisterCommand = new RelayCommand(ExecuteRegister, CanExecuteRegister);
            NavigateToLoginCommand = new RelayCommand(ExecuteNavigateToLogin);
            PasswordChangedCommand = new RelayCommand(ExecutePasswordChanged);
            ConfirmPasswordChangedCommand = new RelayCommand(ExecuteConfirmPasswordChanged);
        }
        #endregion

        #region Command Methods
        private bool CanExecuteRegister(object parameter)
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   Password == ConfirmPassword;
        }

        private void ValidateCanRegister()
        {
            (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ExecuteRegister(object parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                // Kiểm tra định dạng email
                if (!IsValidEmail(Email))
                {
                    ErrorMessage = "Email không hợp lệ.";
                    return;
                }

                // Kiểm tra mật khẩu
                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Mật khẩu xác nhận không khớp.";
                    return;
                }

                // TODO: Thực hiện đăng ký (thay bằng code thực tế của bạn)
                bool isRegistered = RegisterUser(FullName, Email, Password);

                if (isRegistered)
                {
                    MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Chuyển đến trang đăng nhập
                    ExecuteNavigateToLogin(null);
                }
                else
                {
                    ErrorMessage = "Đăng ký không thành công. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng ký: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool RegisterUser(string fullName, string email, string password)
        {
            // TODO: Thực hiện đăng ký người dùng thực tế
            // Ví dụ: return UserManager.Register(fullName, email, password);

            // Giả lập thành công cho mục đích demo
            return true;
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

        private void ExecuteNavigateToLogin(object parameter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Đang chuyển về trang đăng nhập...");

                // Tạo và hiển thị cửa sổ đăng nhập
                var loginWindow = new LightLoginView();
                loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Hiển thị cửa sổ đăng nhập
                loginWindow.Show();
                System.Diagnostics.Debug.WriteLine("Đã hiển thị cửa sổ đăng nhập");

                // Đặt cửa sổ đăng nhập là MainWindow mới của ứng dụng
                Application.Current.MainWindow = loginWindow;

                // Tìm và đóng cửa sổ đăng ký hiện tại
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != loginWindow && window is LightRegisterView)
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
        #endregion
    }
}