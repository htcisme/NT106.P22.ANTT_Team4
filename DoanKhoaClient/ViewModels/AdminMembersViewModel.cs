using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
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
        private bool _isAllSelected;
        private string _password;
        private string _adminCode;

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

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string AdminCode
        {
            get => _adminCode;
            set
            {
                _adminCode = value;
                OnPropertyChanged();
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

        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    if (Users != null)
                    {
                        foreach (var user in Users)
                            user.IsSelected = value;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedItems));
                }
            }
        }

        public bool HasSelectedItems => Users != null && Users.Count(u => u.IsSelected) >= 2;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ViewUserDetailsCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand BatchEditCommand { get; }
        public ICommand BatchDeleteCommand { get; }
        public ICommand PasswordChangedCommand { get; }
        public ICommand AdminCodeChangedCommand { get; }

        public AdminMembersViewModel()
        {
            _userService = new UserService();
            Users = new ObservableCollection<User>();

            // Initialize roles for filtering
            Roles = new ObservableCollection<Role>
            {
                new Role { Value = UserRole.All, DisplayName = "Tất cả" },
                new Role { Value = UserRole.Admin, DisplayName = "Quản trị" },
                new Role { Value = UserRole.User, DisplayName = "Người dùng" }
            };

            // Set default role filter
            SelectedRole = Roles.First();

            // Initialize commands
            RefreshCommand = new RelayCommand(_ => LoadUsersAsync(), _ => !IsLoading);
            EditUserCommand = new RelayCommand(ExecuteEditUser, CanExecuteUserAction);
            DeleteUserCommand = new RelayCommand(ExecuteDeleteUser, CanExecuteUserAction);
            ViewUserDetailsCommand = new RelayCommand(ExecuteViewUserDetails, CanExecuteUserAction);
            AddUserCommand = new RelayCommand(ExecuteAddUser, _ => !IsLoading);
            BatchEditCommand = new RelayCommand(_ => ExecuteBatchEdit(), _ => !IsLoading);
            BatchDeleteCommand = new RelayCommand(_ => ExecuteBatchDelete(), _ => !IsLoading);
            PasswordChangedCommand = new RelayCommand(ExecutePasswordChanged);
            AdminCodeChangedCommand = new RelayCommand(ExecuteAdminCodeChanged);

            // Load users
            LoadUsersAsync();
        }

        private void ExecutePasswordChanged(object parameter)
        {
            if (parameter is System.Windows.Controls.PasswordBox passwordBox)
            {
                Password = passwordBox.Password;
            }
        }

        private void ExecuteAdminCodeChanged(object parameter)
        {
            if (parameter is System.Windows.Controls.PasswordBox passwordBox)
            {
                AdminCode = passwordBox.Password;
            }
        }

        private async void LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var users = await _userService.GetUsersAsync();
                Users = new ObservableCollection<User>(users);
                // Đăng ký PropertyChanged cho từng user để cập nhật HasSelectedItems
                foreach (var user in Users)
                {
                    user.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(User.IsSelected))
                            OnPropertyChanged(nameof(HasSelectedItems));
                    };
                }
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

        private async void ExecuteEditUser(object parameter)
        {
            var user = parameter as User ?? SelectedUser;
            if (user == null) return;

            // Lưu vai trò ban đầu để kiểm tra sau này
            var originalRole = user.Role;

            var editDialog = new EditUserDialog(user);
            if (editDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;

                    // Kiểm tra xem có đang thay đổi từ User lên Admin không
                    var isPromotingToAdmin = (editDialog.User.Role == UserRole.Admin && originalRole != UserRole.Admin);

                    if (isPromotingToAdmin)
                    {
                        // Đảm bảo Admin Code được cung cấp
                        if (string.IsNullOrEmpty(editDialog.AdminCode))
                        {
                            MessageBox.Show("Mã xác thực Admin không được để trống.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Gọi API đặc biệt để cập nhật thành Admin
                        var updatedUser = await _userService.UpdateUserToAdminAsync(user.Id, new
                        {
                            User = editDialog.User,
                            AdminCode = editDialog.AdminCode
                        });

                        // Cập nhật người dùng trong danh sách
                        UpdateUserInCollection(updatedUser);
                    }
                    else
                    {
                        // Cập nhật bình thường nếu không phải thay đổi thành Admin
                        var updatedUser = await _userService.UpdateUserAsync(user.Id, editDialog.User);

                        // Cập nhật người dùng trong danh sách
                        UpdateUserInCollection(updatedUser);
                    }

                    MessageBox.Show("Cập nhật thông tin thành viên thành công!",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi cập nhật: {ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void UpdateUserInCollection(User updatedUser)
        {
            if (updatedUser == null) return;

            // Tìm người dùng trong danh sách Users
            var existingUser = Users.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (existingUser != null)
            {
                int index = Users.IndexOf(existingUser);
                Users[index] = updatedUser;
            }

            // Cập nhật UI
            OnPropertyChanged(nameof(FilteredUsers));
        }

        private async void ExecuteDeleteUser(object parameter)
        {
            var user = parameter as User ?? SelectedUser;
            if (user == null) return;

            if(user.Id == _userService.CurrentUser.Id)
            {
                MessageBox.Show("Bạn không thể xóa chính mình!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Bạn có chắc chắn muốn xóa thành viên này?",
                "Xác nhận xóa", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await _userService.DeleteUserAsync(user.Id);
                    Users.Remove(user);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
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
                $"Tên hiển thị: {user.DisplayName}\n" +
                $"Email: {user.Email}\n" +
                $"Thời gian hoạt động: {user.LastSeen}\n" +
                $"Số hoạt động: {user.ActivitiesCount}\n" +
                $"Trạng thái: {(user.EmailVerified ? "Đã xác thực" : "Chưa xác thực")}\n" +
                $"Thời gian xác thực: {user.EmailVerificationCodeExpiry?.ToString("g") ?? "Chưa xác thực"}\n" +
                $"Vai trò: {user.Role}",
                "Thông tin người dùng", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteAddUser(object parameter)
        {
            var addUserDialog = new AddUserDialog();

            if (addUserDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;

                    // Lấy thông tin từ Result
                    var result = addUserDialog.Result;
                    var user = result.User;

                    var request = new RegisterRequest
                    {
                        Username = user.Username,
                        DisplayName = user.DisplayName,
                        Email = user.Email,
                        Password = !string.IsNullOrEmpty(result.Password) ? result.Password : "123456",
                        Role = user.Role,
                        AdminCode = result.AdminCode
                    };

                    var createdUser = await _userService.CreateUserAsync(request);
                    Users.Add(createdUser);
                    OnPropertyChanged(nameof(FilteredUsers));
                    MessageBox.Show("Thêm thành viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi thêm thành viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecuteBatchEdit()
        {
           
            var selectedUsers = Users.Where(u => u.IsSelected).ToList();
            if (selectedUsers.Count < 2)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 2 thành viên để sửa hàng loạt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (selectedUsers.Any(u => u.Id == _userService.CurrentUser?.Id))
            {
                MessageBox.Show("Bạn không thể xóa chính mình!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show($"Bạn có chắc muốn sửa {selectedUsers.Count} thành viên đã chọn?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var dialog = new BatchEditUserDialog();
                if (dialog.ShowDialog() == true)
                {
                    // TODO: Áp dụng các thay đổi cho selectedUsers dựa trên dialog.EditOptions
                    MessageBox.Show("Đã cập nhật thông tin cho các thành viên đã chọn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void ExecuteBatchDelete()
        {
            var selectedUsers = Users.Where(u => u.IsSelected).ToList();
            if (selectedUsers.Count < 2)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 2 thành viên để xóa hàng loạt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc muốn xóa {selectedUsers.Count} thành viên đã chọn?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;

                    // Tạo danh sách lỗi nếu có
                    var errors = new List<string>();

                    // Xóa từng user trên server
                    foreach (var user in selectedUsers.ToList())
                    {
                        try
                        {
                            await _userService.DeleteUserAsync(user.Id);
                            Users.Remove(user);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Không thể xóa {user.DisplayName}: {ex.Message}");
                        }
                    }

                    // Hiển thị thông báo thành công hoặc lỗi
                    if (errors.Count > 0)
                    {
                        string errorMessage = "Một số người dùng không thể xóa:\n" + string.Join("\n", errors);
                        MessageBox.Show(errorMessage, "Lỗi khi xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Đã xóa tất cả thành viên đã chọn!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    OnPropertyChanged(nameof(HasSelectedItems));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
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