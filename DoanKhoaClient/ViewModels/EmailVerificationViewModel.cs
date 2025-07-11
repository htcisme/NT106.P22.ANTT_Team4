using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.ViewModels
{
    public class EmailVerificationViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private string _userId;
        private string _username;
        private string _email;
        private string _verificationCode;
        private string _errorMessage;
        private bool _isLoading;

        public string UserId
        {
            get => _userId;
            set
            {
                if (_userId != value)
                {
                    _userId = value;
                    OnPropertyChanged();
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
                }
            }
        }

        public string VerificationCode
        {
            get => _verificationCode;
            set
            {
                if (_verificationCode != value)
                {
                    _verificationCode = value;
                    OnPropertyChanged();
                    (VerifyEmailCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                    (VerifyEmailCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ResendCodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand VerifyEmailCommand { get; private set; }
        public ICommand ResendCodeCommand { get; private set; }
        public ICommand BackToLoginCommand { get; private set; }
        public ICommand NavigateToLoginCommand { get; private set; }

        public EmailVerificationViewModel()
        {
            _authService = new AuthService();
            NavigateToLoginCommand = new RelayCommand(ExecuteNavigateToLogin);


            VerifyEmailCommand = new RelayCommand(ExecuteVerifyEmail, CanExecuteVerifyEmail);
            ResendCodeCommand = new RelayCommand(ExecuteResendCode, CanExecuteResendCode);
            BackToLoginCommand = new RelayCommand(ExecuteBackToLogin);
        }
        private void ExecuteNavigateToLogin(object parameter)
        {
            try
            {
                // Đóng cửa sổ verification hiện tại
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is EmailVerificationView)
                    {
                        window.DialogResult = false; // Set dialog result
                        window.Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing verification window: {ex.Message}");
            }
        }

        private bool CanExecuteVerifyEmail(object parameter)
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(VerificationCode) && VerificationCode.Length >= 6;
        }

        private bool CanExecuteResendCode(object parameter)
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Email);
        }

        private async void ExecuteVerifyEmail(object parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var response = await _authService.VerifyEmailAsync(UserId, VerificationCode);

                if (response != null && !string.IsNullOrEmpty(response.Id))
                {
                    MessageBox.Show("Email đã được xác thực thành công! Vui lòng đăng nhập.",
                        "Xác thực thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Redirect to login
                    var loginView = new LoginView();
                    loginView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    loginView.Show();

                    // Close this window
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is EmailVerificationView)
                        {
                            window.Close();
                            break;
                        }
                    }
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Không thể xác thực email. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteResendCode(object parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // This would typically call an API endpoint to resend the code
                // For now, we'll just show a message
                MessageBox.Show($"Mã xác thực mới đã được gửi đến {Email}",
                    "Đã gửi mã", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteBackToLogin(object parameter)
        {
            var loginView = new LoginView();
            loginView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            loginView.Show();

            // Close this window
            foreach (Window window in Application.Current.Windows)
            {
                if (window is EmailVerificationView)
                {
                    window.Close();
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // RelayCommand implementation
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
    }
}