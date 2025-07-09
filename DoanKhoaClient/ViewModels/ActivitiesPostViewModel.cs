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
using System.Collections.ObjectModel;
using DoanKhoaClient.Views;

namespace DoanKhoaClient.ViewModels
{
    public class ActivitiesPostViewModel : INotifyPropertyChanged
    {
        private readonly ActivityService _activityService;
        private readonly UserService _userService;
        private ObservableCollection<Activity> _activities;

        private Activity _activity;
        private bool _isLoading;
        private string _errorMessage;
        private CancellationTokenSource _cts;
        private ObservableCollection<Activity> _searchResults;
        private string _searchText;
        private ObservableCollection<SearchField> _searchFields;
        private SearchField _selectedSearchField;
        private bool _isSearchResultOpen;
        private bool _isSearchDropdownOpen;
        public bool IsSearchDropdownOpen
        {
            get => _isSearchDropdownOpen;
            set { _isSearchDropdownOpen = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Activity> Activities
        {
            get => _activities;
            set { _activities = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Activity> SearchResults
        {
            get => _searchResults;
            set { _searchResults = value; OnPropertyChanged(); }
        }
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                SearchActivities(); // Gọi hàm search riêng thay vì FilterActivities
            }
        }

        public ObservableCollection<SearchField> SearchFields
        {
            get => _searchFields;
            set { _searchFields = value; OnPropertyChanged(); }
        }

     
        public bool IsSearchResultOpen
        {
            get => _isSearchResultOpen;
            set { _isSearchResultOpen = value; OnPropertyChanged(); }
        }



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
        public ICommand OpenActivityDetailCommand { get; }


        public ActivitiesPostViewModel(Activity activity, UserService userService = null, ActivityService activityService = null)
        {
            _activityService = activityService ?? new ActivityService();
            _userService = userService ?? new UserService(_activityService);
            Activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _cts = new CancellationTokenSource();

            Activities = new ObservableCollection<Activity>();
            SearchResults = new ObservableCollection<Activity>();

            LoadActivitiesForSearch();

            // Set up commands
            ParticipateCommand = new RelayCommand(ExecuteParticipate, CanExecuteParticipate);
            LikeCommand = new RelayCommand(ExecuteLike, CanExecuteLike);
            BackCommand = new RelayCommand(ExecuteBack, CanExecuteBack);

            // Subscribe to activity status changes
            _userService.ActivityStatusChanged += UserService_ActivityStatusChanged;

            // Update activity status based on user's cached data
            UpdateActivityStatus();

            OpenActivityDetailCommand = new RelayCommand(param => ExecuteOpenActivityDetail(param as Activity), param => param is Activity);
        }

        private async void LoadActivitiesForSearch()
        {
            try
            {
                var activities = await _activityService.GetActivitiesAsync();
                Activities = new ObservableCollection<Activity>(activities ?? new List<Activity>());
            }
            catch (Exception ex)
            {
                // Log error but don't break the UI
                Activities = new ObservableCollection<Activity>();
            }
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
        private void ExecuteOpenActivityDetail(Activity activity)
        {
            if (activity == null) return;

            // Đóng search results trước
            IsSearchResultOpen = false;

            // Nếu đang xem activity khác, mở window mới
            if (activity.Id != Activity.Id)
            {
                var postView = new ActivitiesPostView(activity);
                postView.Show();

                // Đóng window hiện tại
                Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this)?.Close();
            }
        }

        public void SearchActivities()
        {
            if (Activities == null) return;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchResults = Activities.Where(a =>
                    a.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    a.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                SearchResults = new ObservableCollection<Activity>(searchResults);
                IsSearchResultOpen = true;
            }
            else
            {
                SearchResults.Clear();
                IsSearchResultOpen = false;
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

        public class SearchField
        {
            public string DisplayName { get; set; }
            public string Field { get; set; }
        }
    }
}