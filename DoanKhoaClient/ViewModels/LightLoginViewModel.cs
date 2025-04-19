using DoanKhoaClient.Views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DoanKhoaClient.ViewModels
{
    public partial class LightLoginViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _username;
        private string _password;
        private string _errorMessage;
        private bool _isLoading;
        private Visibility _usernamePlaceholderVisibility = Visibility.Visible;
        private Visibility _passwordPlaceholderVisibility = Visibility.Visible;
        #endregion

        #region Properties
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
                    ValidateCanLogin();
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
                    ValidateCanLogin();
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
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
        #endregion

        #region Commands
        public ICommand LoginCommand { get; private set; }
        public ICommand NavigateToRegisterCommand { get; private set; }
        public ICommand ForgotPasswordCommand { get; private set; }
        public ICommand PasswordChangedCommand { get; private set; }
        #endregion

        #region Constructor
        public LightLoginViewModel()
        {
            // Initialize commands
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            NavigateToRegisterCommand = new RelayCommand(ExecuteNavigateToRegister);
            ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
            PasswordChangedCommand = new RelayCommand(ExecutePasswordChanged);
        }
        #endregion

        #region Command Methods
        private bool CanExecuteLogin(object parameter)
        {
            return !IsLoading &&
                  !string.IsNullOrWhiteSpace(Username) &&
                  !string.IsNullOrWhiteSpace(Password);
        }

        private void ValidateCanLogin()
        {
            (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ExecuteLogin(object parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Authenticate user (replace with your actual authentication logic)
                bool isAuthenticated = AuthenticateUser(Username, Password);

                if (isAuthenticated)
                {
                    MessageBox.Show("Đăng nhập thành công!");

                    // Navigate to dashboard (add your navigation logic here)
                    // Example:
                    // var dashboardWindow = new LightDashboardView();
                    // dashboardWindow.Show();
                    // Window.GetWindow(this)?.Close();
                }
                else
                {
                    ErrorMessage = "Tên đăng nhập hoặc mật khẩu sai.";
                    MessageBox.Show(ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng nhập: {ex.Message}";
                MessageBox.Show(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool AuthenticateUser(string username, string password)
        {
            // TODO: Implement actual authentication
            // Example: return UserManager.Login(username, password);

            // For testing purposes - replace with your authentication logic
            return (username == "admin" && password == "admin");
        }

        private void ExecuteNavigateToRegister(object parameter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Đang chuyển đến trang đăng ký...");

                // Tạo window mới
                var registerWindow = new LightRegisterView();
                registerWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Hiển thị cửa sổ mới
                registerWindow.Show();

                // Tìm cửa sổ hiện tại bằng cách liệt kê tất cả cửa sổ - KHÔNG sử dụng parameter
                Window currentWindow = null;

                foreach (Window window in Application.Current.Windows)
                {
                    // Tìm cửa sổ đăng nhập (không phải cửa sổ đăng ký vừa tạo)
                    if (window != registerWindow && window is LightLoginView)
                    {
                        currentWindow = window;
                        break;
                    }
                }

                // Nếu tìm thấy cửa sổ đăng nhập, đóng lại
                if (currentWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine("Đóng cửa sổ đăng nhập hiện tại");
                    currentWindow.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Lỗi khi chuyển trang: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteForgotPassword(object parameter)
        {
            MessageBox.Show("Đi tới trang quên mật khẩu.");
            // Implement navigation to forgot password page
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