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
using System.Net.Http;

namespace DoanKhoaClient.ViewModels
{
    public class ActivitiesPostViewModel : INotifyPropertyChanged
    {
        private readonly ActivityService _activityService;
        private readonly UserService _userService;
        private readonly CommentService _commentService;
        private ObservableCollection<Activity> _activities;
        private ObservableCollection<Comment> _comments = new ObservableCollection<Comment>();

        private Activity _activity;
        private bool _isLoading;
        private string _errorMessage;
        private CancellationTokenSource _cts;
        private ObservableCollection<Activity> _searchResults;
        private string _searchText;
        private bool _isSearchResultOpen;
        private bool _isSearchDropdownOpen;
        private string _newCommentText;
        private Comment _replyingToComment;
        private bool _isLoadingComments;

        public int CommentsCount => Comments?.Count ?? 0;
        public int ActivitiesCount => Activities?.Count ?? 0;
        public int SearchResultsCount => SearchResults?.Count ?? 0;

        public bool IsSearchDropdownOpen
        {
            get => _isSearchDropdownOpen;
            set { _isSearchDropdownOpen = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Activity> Activities
        {
            get => _activities;
            set
            {
                if (_activities != null)
                {
                    _activities.CollectionChanged -= Activities_CollectionChanged;
                }

                _activities = value ?? new ObservableCollection<Activity>();

                _activities.CollectionChanged += Activities_CollectionChanged;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ActivitiesCount));
            }
        }

        public ObservableCollection<Activity> SearchResults
        {
            get => _searchResults;
            set
            {
                if (_searchResults != null)
                {
                    _searchResults.CollectionChanged -= SearchResults_CollectionChanged;
                }

                _searchResults = value ?? new ObservableCollection<Activity>();

                _searchResults.CollectionChanged += SearchResults_CollectionChanged;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SearchResultsCount));
            }
        }

        // Event handlers for collection changes
        private void Comments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(CommentsCount));
        }

        private void Activities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ActivitiesCount));
        }

        private void SearchResults_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SearchResultsCount));
        }

        public ObservableCollection<Comment> Comments
        {
            get => _comments;
            set
            {
                if (_comments != null)
                {
                    _comments.CollectionChanged -= Comments_CollectionChanged;
                }

                _comments = value ?? new ObservableCollection<Comment>();

                _comments.CollectionChanged += Comments_CollectionChanged;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CommentsCount));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                SearchActivities();
            }
        }

        public string NewCommentText
        {
            get => _newCommentText;
            set { _newCommentText = value; OnPropertyChanged(); }
        }

        public Comment ReplyingToComment
        {
            get => _replyingToComment;
            set { _replyingToComment = value; OnPropertyChanged(); }
        }

        public bool IsLoadingComments
        {
            get => _isLoadingComments;
            set { _isLoadingComments = value; OnPropertyChanged(); }
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
        public ICommand PostCommentCommand { get; }
        public ICommand ReplyToCommentCommand { get; }
        public ICommand LikeCommentCommand { get; }
        public ICommand DeleteCommentCommand { get; }
        public ICommand CancelReplyCommand { get; }

        public ActivitiesPostViewModel(Activity activity, UserService userService = null, ActivityService activityService = null)
        {
            // Validate input
            Activity = activity ?? throw new ArgumentNullException(nameof(activity));
            if (string.IsNullOrEmpty(activity.Id))
            {
                throw new ArgumentException("Activity ID cannot be null or empty", nameof(activity));
            }

            _activityService = activityService ?? new ActivityService();
            _userService = userService ?? new UserService(_activityService);
            _commentService = new CommentService();
            _cts = new CancellationTokenSource();

            Activities = new ObservableCollection<Activity>();
            SearchResults = new ObservableCollection<Activity>();
            Comments = new ObservableCollection<Comment>();

            LoadActivitiesForSearch();
            LoadComments();

            // Set up commands
            ParticipateCommand = new RelayCommand(ExecuteParticipate, CanExecuteParticipate);
            LikeCommand = new RelayCommand(ExecuteLike, CanExecuteLike);
            BackCommand = new RelayCommand(ExecuteBack, CanExecuteBack);
            PostCommentCommand = new RelayCommand(ExecutePostComment, CanExecutePostComment);
            ReplyToCommentCommand = new RelayCommand(ExecuteReplyToComment, CanExecuteReplyToComment);
            LikeCommentCommand = new RelayCommand(ExecuteLikeComment, CanExecuteLikeComment);
            DeleteCommentCommand = new RelayCommand(ExecuteDeleteComment, CanExecuteDeleteComment);
            CancelReplyCommand = new RelayCommand(ExecuteCancelReply, CanExecuteCancelReply);

            // Subscribe to activity status changes
            _userService.ActivityStatusChanged += UserService_ActivityStatusChanged;

            // Update activity status based on user's cached data
            UpdateActivityStatus();


            OpenActivityDetailCommand = new RelayCommand(param => ExecuteOpenActivityDetail(param as Activity), param => param is Activity);

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity), "Activity cannot be null");
            }

            if (string.IsNullOrEmpty(activity.Id))
            {
                throw new ArgumentException("Activity ID cannot be null or empty", nameof(activity));
            }

            Console.WriteLine($"DEBUG - Activity loaded: ID={activity.Id}, Title={activity.Title}");

            // Kiểm tra UserId trong Settings
            var currentUserId = DoanKhoaClient.Properties.Settings.Default.CurrentUserId;
            Console.WriteLine($"DEBUG - Current User ID from Settings: {currentUserId ?? "null"}");

            if (string.IsNullOrEmpty(currentUserId))
            {
                Console.WriteLine("WARNING - No user ID found in settings, setting default");
                DoanKhoaClient.Properties.Settings.Default.CurrentUserId = "676b4e0e2d5a8b1234567890";
                DoanKhoaClient.Properties.Settings.Default.Save();
            }
        }

        private bool ValidateCommentData(CreateCommentRequest request)
        {
            if (string.IsNullOrEmpty(request.ActivityId))
            {
                MessageBox.Show("Lỗi: Không tìm thấy ID hoạt động.", "Lỗi dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrEmpty(request.UserId))
            {
                MessageBox.Show("Lỗi: Không tìm thấy ID người dùng.", "Lỗi dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                MessageBox.Show("Vui lòng nhập nội dung bình luận.", "Thiếu dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Kiểm tra độ dài content
            if (request.Content.Length > 1000)
            {
                MessageBox.Show("Nội dung bình luận quá dài (tối đa 1000 ký tự).", "Dữ liệu không hợp lệ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
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
                Activities = new ObservableCollection<Activity>();
            }
        }

        private async void LoadComments()
        {
            try
            {
                IsLoadingComments = true;
                ErrorMessage = null;

                var comments = await _commentService.GetCommentsByActivityIdAsync(Activity.Id, _cts.Token);
                Comments = new ObservableCollection<Comment>(comments ?? new List<Comment>());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải bình luận: {ex.Message}";
                Comments = new ObservableCollection<Comment>();
            }
            finally
            {
                IsLoadingComments = false;
            }
        }

        private void UserService_ActivityStatusChanged(object sender, UserActivityStatusChangedEventArgs e)
        {
            if (Activity != null && Activity.Id == e.ActivityId)
            {
                if (e.StatusType == ActivityStatusType.Participation)
                {
                    Activity.IsParticipated = e.IsParticipated;
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

        // Comment-related methods
        private bool CanExecutePostComment(object parameter)
        {
            return !IsLoading && !IsLoadingComments && !string.IsNullOrWhiteSpace(NewCommentText);
        }

        private async void ExecutePostComment(object parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                if (string.IsNullOrWhiteSpace(NewCommentText))
                {
                    MessageBox.Show("Vui lòng nhập nội dung bình luận.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var currentUserId = DoanKhoaClient.Properties.Settings.Default.CurrentUserId;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    MessageBox.Show("Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var request = new CreateCommentRequest
                {
                    ActivityId = Activity.Id,
                    UserId = currentUserId,
                    Content = NewCommentText.Trim(),
                    ParentCommentId = ReplyingToComment?.Id
                };

                // Log thông tin debug
                System.Diagnostics.Debug.WriteLine($"=== DEBUG COMMENT REQUEST ===");
                System.Diagnostics.Debug.WriteLine($"ActivityId: {request.ActivityId}");
                System.Diagnostics.Debug.WriteLine($"UserId: {request.UserId}");
                System.Diagnostics.Debug.WriteLine($"Content: {request.Content}");
                System.Diagnostics.Debug.WriteLine($"ParentCommentId: {request.ParentCommentId ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"IsReply: {ReplyingToComment != null}");

                var newComment = await _commentService.CreateCommentAsync(request, _cts.Token);

                if (newComment != null)
                {
                    Comments.Add(newComment);
                    NewCommentText = string.Empty;
                    ReplyingToComment = null;

                    var successMessage = request.ParentCommentId != null ?
                        "Phản hồi đã được gửi thành công!" :
                        "Bình luận đã được đăng thành công!";

                    MessageBox.Show(successMessage, "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception("Server không trả về comment đã tạo");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                System.Diagnostics.Debug.WriteLine($"=== ERROR DETAILS ===");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                ErrorMessage = $"Lỗi khi đăng bình luận: {ex.Message}";

                // Hiển thị lỗi THẬT SỰ thay vì thông báo chung chung
                string userMessage;

                if (ex.Message.Contains("kết nối") || ex.Message.Contains("connection") || ex.Message.Contains("No connection"))
                {
                    userMessage = "Không thể kết nối đến server. Vui lòng kiểm tra:\n" +
                                 "- Server có đang chạy không?\n" +
                                 "- Kết nối mạng có ổn định không?";
                }
                else if (ex.Message.Contains("timeout") || ex.Message.Contains("hết thời gian"))
                {
                    userMessage = "Yêu cầu hết thời gian chờ. Vui lòng thử lại.";
                }
                else if (ex.Message.Contains("validation") || ex.Message.Contains("required") || ex.Message.Contains("Invalid"))
                {
                    userMessage = $"Dữ liệu không hợp lệ:\n{ex.Message}";
                }
                else if (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
                {
                    userMessage = "API comment không tồn tại trên server. Vui lòng kiểm tra server có implement comment API không.";
                }
                else if (ex.Message.Contains("500") || ex.Message.Contains("Internal Server Error"))
                {
                    userMessage = "Server gặp lỗi nội bộ. Vui lòng kiểm tra server logs.";
                }
                else
                {
                    // Hiển thị lỗi gốc để có thể debug
                    userMessage = $"Lỗi: {ex.Message}";
                }

                MessageBox.Show(userMessage, "Lỗi đăng bình luận",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }



        private bool CanExecuteReplyToComment(object parameter)
        {
            return !IsLoading && parameter is Comment;
        }

        private void ExecuteReplyToComment(object parameter)
        {
            if (parameter is Comment comment)
            {
                ReplyingToComment = comment;
                // Focus on comment input (this would need to be handled in the view)
            }
        }

        private bool CanExecuteLikeComment(object parameter)
        {
            return !IsLoading && parameter is Comment;
        }

        private async void ExecuteLikeComment(object parameter)
        {
            if (!(parameter is Comment comment)) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                bool originalStatus = comment.IsLiked;
                int originalCount = comment.LikeCount;

                // Optimistic update
                comment.IsLiked = !originalStatus;
                comment.LikeCount += comment.IsLiked ? 1 : -1;

                bool result = await _commentService.ToggleCommentLikeAsync(comment.Id, _cts.Token);

                if (!result)
                {
                    // Revert changes if failed
                    comment.IsLiked = originalStatus;
                    comment.LikeCount = originalCount;
                    MessageBox.Show("Không thể thay đổi trạng thái yêu thích bình luận.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi thay đổi trạng thái yêu thích: {ex.Message}";
                MessageBox.Show($"Thao tác không thành công: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteDeleteComment(object parameter)
        {
            return !IsLoading && parameter is Comment comment && comment.IsOwner;
        }

        private async void ExecuteDeleteComment(object parameter)
        {
            if (!(parameter is Comment comment)) return;

            var result = MessageBox.Show("Bạn có chắc muốn xóa bình luận này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await _commentService.DeleteCommentAsync(comment.Id, _cts.Token);

                // Remove from local collection
                Comments.Remove(comment);

                MessageBox.Show("Bình luận đã được xóa thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi xóa bình luận: {ex.Message}";
                MessageBox.Show($"Không thể xóa bình luận: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteCancelReply(object parameter)
        {
            return ReplyingToComment != null;
        }

        private void ExecuteCancelReply(object parameter)
        {
            ReplyingToComment = null;
            NewCommentText = string.Empty;
        }

        // Existing methods...
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

                bool originalStatus = Activity.IsParticipated;
                bool result = await _userService.ToggleActivityParticipationAsync(Activity.Id);

                if (!result)
                {
                    Activity.IsParticipated = originalStatus;
                    ErrorMessage = "Không thể thay đổi trạng thái tham gia. Vui lòng thử lại.";
                    MessageBox.Show("Thao tác không thành công. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (Activity.IsParticipated)
                {
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

                string userId = _userService.GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    ErrorMessage = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                    MessageBox.Show(ErrorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool result = await _userService.ToggleActivityLikeAsync(Activity.Id);

                if (!result)
                {
                    Activity.IsLiked = !Activity.IsLiked;
                    ErrorMessage = "Không thể thay đổi trạng thái yêu thích. Vui lòng thử lại.";
                    MessageBox.Show(ErrorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
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

            IsSearchResultOpen = false;

            if (activity.Id != Activity.Id)
            {
                var postView = new ActivitiesPostView(activity);
                postView.Show();

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
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this)?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Cleanup()
        {
            _cts?.Cancel();
            _userService.ActivityStatusChanged -= UserService_ActivityStatusChanged;
        }

        public class SearchField
        {
            public string DisplayName { get; set; }
            public string Field { get; set; }
        }
    }
}