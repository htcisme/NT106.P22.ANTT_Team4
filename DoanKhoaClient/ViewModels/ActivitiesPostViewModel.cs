using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Threading;

namespace DoanKhoaClient.ViewModels
{
    public class ActivitiesPostViewModel : INotifyPropertyChanged
    {
        private readonly ActivityService _activityService;
        private readonly UserService _userService;
        private Activity _activity;
        private bool _isLoading;
        private string _errorMessage;
        private CancellationTokenSource _cts;

        public Activity Activity
        {
            get => _activity;
            set
            {
                _activity = value;
                OnPropertyChanged();
                // Refresh command can-execute status
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                // Refresh command can-execute status
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand ParticipateCommand { get; }
        public ICommand LikeCommand { get; }
        public ICommand BackCommand { get; }

        public ActivitiesPostViewModel(Activity activity, UserService userService = null, ActivityService activityService = null)
        {
            _activityService = activityService ?? new ActivityService();
            _userService = userService ?? new UserService(_activityService);
            Activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _cts = new CancellationTokenSource();

            // Set up commands
            ParticipateCommand = new RelayCommand(ExecuteParticipate, CanExecuteParticipate);
            LikeCommand = new RelayCommand(ExecuteLike, CanExecuteLike);
            BackCommand = new RelayCommand(ExecuteBack, CanExecuteBack);

            // Subscribe to activity status changes
            _userService.ActivityStatusChanged += UserService_ActivityStatusChanged;

            // Update activity status based on user's cached data
            UpdateActivityStatus();
        }

        private void UserService_ActivityStatusChanged(object sender, UserActivityStatusChangedEventArgs e)
        {
            // Only update if this is the activity we're viewing
            if (Activity != null && Activity.Id == e.ActivityId)
            {
                if (e.StatusType == ActivityStatusType.Participation)
                {
                    Activity.IsParticipated = e.IsParticipated;
                    // Update participation count
                    if (e.IsParticipated)
                    {
                        Activity.ParticipantCount++;
                    }
                    else
                    {
                        Activity.ParticipantCount = Math.Max(0, Activity.ParticipantCount - 1);
                    }
                }
                else if (e.StatusType == ActivityStatusType.Like)
                {
                    Activity.IsLiked = e.IsLiked;
                    // Update like count
                    if (e.IsLiked)
                    {
                        Activity.LikeCount++;
                    }
                    else
                    {
                        Activity.LikeCount = Math.Max(0, Activity.LikeCount - 1);
                    }
                }
            }
        }

        private void UpdateActivityStatus()
        {
            if (Activity != null)
            {
                Activity.IsParticipated = _userService.IsActivityParticipated(Activity.Id);
                Activity.IsLiked = _userService.IsActivityLiked(Activity.Id);
            }
        }

        private bool CanExecuteParticipate(object parameter)
        {
            return !IsLoading && Activity != null;
        }

        private void ExecuteParticipate(object parameter)
        {
            ToggleParticipation();
        }

        private async void ToggleParticipation()
        {
            if (Activity == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Store original status for reverting if needed
                bool originalStatus = Activity.IsParticipated;

                // Use UserService instead of ActivityService directly
                bool result = await _userService.ToggleActivityParticipationAsync(Activity.Id);

                if (!result)
                {
                    // If the API call failed, revert the UI changes
                    Activity.IsParticipated = originalStatus;
                    ErrorMessage = "Không thể thay đổi trạng thái tham gia. Vui lòng thử lại.";
                    MessageBox.Show("Thao tác không thành công. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (Activity.IsParticipated)
                {
                    // Show success message only when registering, not when unregistering
                    MessageBox.Show("Đăng ký tham gia hoạt động thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Hủy đăng ký tham gia hoạt động thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Thao tác không thành công: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteLike(object parameter)
        {
            return !IsLoading && Activity != null;
        }

        private void ExecuteLike(object parameter)
        {
            ToggleLike();
        }

        private async void ToggleLike()
        {
            if (Activity == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Get userId from UserService
                string userId = _userService.GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    ErrorMessage = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                    MessageBox.Show(ErrorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Use UserService instead of ActivityService directly
                bool result = await _userService.ToggleActivityLikeAsync(Activity.Id);

                if (!result)
                {
                    // If the API call failed, revert the UI changes
                    Activity.IsLiked = !Activity.IsLiked;
                    ErrorMessage = "Không thể thay đổi trạng thái yêu thích. Vui lòng thử lại.";
                    MessageBox.Show(ErrorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Show success message when the action is successful
                    if (Activity.IsLiked)
                    {
                        MessageBox.Show("Đã thêm vào danh sách yêu thích!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Đã xóa khỏi danh sách yêu thích!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Thao tác không thành công: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteBack(object parameter)
        {
            return !IsLoading;
        }

        private void ExecuteBack(object parameter)
        {
            // Close the window
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this)?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Cleanup()
        {
            // Cancel any ongoing operations
            _cts?.Cancel();

            // Unsubscribe from events
            _userService.ActivityStatusChanged -= UserService_ActivityStatusChanged;
        }
    }
}