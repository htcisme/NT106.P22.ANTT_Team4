using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.ViewModels
{
    public class AdminMembersViewModel : INotifyPropertyChanged
    {
        private readonly UserService _userService;
        private ObservableCollection<User> _users;
        private string _searchTerm;
        private User _selectedUser;
        private Role _selectedRole;
        private bool _isLoading;
        private string _errorMessage;

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredUsers));
            }
        }

        public IEnumerable<User> FilteredUsers
        {
            get
            {
                if (Users == null) return new List<User>();

                var filtered = Users.AsEnumerable();

                // Filter by search term
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    filtered = filtered.Where(u =>
                        u.Username.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        u.DisplayName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by role
                if (SelectedRole != null && SelectedRole.Value != UserRole.All)
                {
                    filtered = filtered.Where(u => u.Role == SelectedRole.Value);
                }

                return filtered;
            }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredUsers));
            }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<Role> Roles { get; }

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredUsers));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ViewUserDetailsCommand { get; }

        public AdminMembersViewModel()
        {
            _userService = new UserService();
            Users = new ObservableCollection<User>();

            // Initialize roles for filtering
            Roles = new ObservableCollection<Role>
            {
                new Role { Value = UserRole.All, DisplayName = "Tất cả" },
                new Role { Value = UserRole.Admin, DisplayName = "Admin" },
                new Role { Value = UserRole.User, DisplayName = "Người dùng" }
            };

            // Set default role filter
            SelectedRole = Roles.First();

            // Initialize commands
            RefreshCommand = new RelayCommand(_ => LoadUsersAsync(), _ => !IsLoading);
            EditUserCommand = new RelayCommand(ExecuteEditUser, CanExecuteUserAction);
            DeleteUserCommand = new RelayCommand(ExecuteDeleteUser, CanExecuteUserAction);
            ViewUserDetailsCommand = new RelayCommand(ExecuteViewUserDetails, CanExecuteUserAction);

            // Load users
            LoadUsersAsync();
        }

        private async void LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var users = await _userService.GetUsersAsync();
                Users = new ObservableCollection<User>(users);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể tải danh sách người dùng: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteUserAction(object parameter)
        {
            return !IsLoading && (parameter as User != null || SelectedUser != null);
        }

        private void ExecuteEditUser(object parameter)
        {
            var user = parameter as User ?? SelectedUser;
            if (user == null) return;

            // TODO: Implement edit user functionality
            MessageBox.Show($"Chức năng sửa thông tin người dùng {user.DisplayName} sẽ được phát triển trong phiên bản sau.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteDeleteUser(object parameter)
        {
            var user = parameter as User ?? SelectedUser;
            if (user == null) return;

            // Confirm deletion
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa người dùng {user.DisplayName}?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // TODO: Implement delete user functionality
                MessageBox.Show($"Chức năng xóa người dùng sẽ được phát triển trong phiên bản sau.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteViewUserDetails(object parameter)
        {
            var user = parameter as User ?? SelectedUser;
            if (user == null) return;

            // TODO: Implement view user details
            MessageBox.Show($"Chi tiết người dùng {user.DisplayName}:\n" +
                $"ID: {user.Id}\n" +
                $"Tên đăng nhập: {user.Username}\n" +
                $"Email: {user.Email}\n" +
                $"Vai trò: {user.Role}",
                "Thông tin người dùng", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class Role
        {
            public UserRole Value { get; set; }
            public string DisplayName { get; set; }
        }

        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                _execute = execute;
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

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}