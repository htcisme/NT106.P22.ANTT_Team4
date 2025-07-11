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

        public string CurrentUserAvatar
        {
            get
            {
                try
                {
                    // Lấy từ current user service
                    var avatar = _userService?.GetCurrentUserAvatar();
                    if (!string.IsNullOrEmpty(avatar))
                        return avatar;

                    // Fallback từ App properties
                    if (Application.Current?.Properties.Contains("CurrentUser") == true &&
                        Application.Current.Properties["CurrentUser"] is User currentUser &&
                        !string.IsNullOrEmpty(currentUser.AvatarUrl))
                    {
                        return currentUser.AvatarUrl;
                    }

                    // Avatar mặc định
                    return "/Views/Images/User-icon.png";
                }
                catch
                {
                    return "/Views/Images/User-icon.png";
                }
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

                System.Diagnostics.Debug.WriteLine($"=== LOADING COMMENTS FOR ACTIVITY: {Activity.Id} ===");

                var comments = await _commentService.GetCommentsByActivityIdAsync(Activity.Id, _cts.Token);

                System.Diagnostics.Debug.WriteLine($"Loaded {comments?.Count ?? 0} comments from server");

                if (comments != null)
                {
                    // Process comments để đảm bảo data đầy đủ
                    foreach (var comment in comments)
                    {
                        // Đảm bảo avatar
                        if (string.IsNullOrEmpty(comment.UserAvatar))
                        {
                            comment.UserAvatar = "/Views/Images/User-icon.png";
                        }

                        // Đảm bảo user display name
                        if (string.IsNullOrEmpty(comment.UserDisplayName))
                        {
                            comment.UserDisplayName = "Unknown User";
                        }

                        // Set IsOwner cho comments
                        var currentUserId = _userService?.GetCurrentUserId();
                        if (!string.IsNullOrEmpty(currentUserId))
                        {
                            comment.IsOwner = comment.UserId == currentUserId;
                        }

                        System.Diagnostics.Debug.WriteLine($"Comment: {comment.Id}, User: {comment.UserDisplayName}, IsReply: {comment.IsReply}, ParentId: {comment.ParentCommentId ?? "null"}");
                    }

                    // Đảm bảo reply information được populate đúng cách
                    PopulateReplyInformationLocally(comments);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Comments = new ObservableCollection<Comment>(comments ?? new List<Comment>());
                    System.Diagnostics.Debug.WriteLine($"UI updated with {Comments.Count} comments");
                    OnPropertyChanged(nameof(Comments));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading comments: {ex.Message}");
                ErrorMessage = $"Lỗi khi tải bình luận: {ex.Message}";
                Comments = new ObservableCollection<Comment>();
            }
            finally
            {
                IsLoadingComments = false;
            }
        }

        private void PopulateReplyInformationLocally(List<Comment> comments)
        {
            var commentDict = comments.ToDictionary(c => c.Id, c => c);

            foreach (var comment in comments.Where(c => !string.IsNullOrEmpty(c.ParentCommentId)))
            {
                if (commentDict.TryGetValue(comment.ParentCommentId, out var parentComment))
                {
                    // Chỉ set nếu chưa có thông tin
                    if (string.IsNullOrEmpty(comment.ReplyToUserName))
                    {
                        comment.ReplyToUserName = parentComment.UserDisplayName;
                    }

                    if (string.IsNullOrEmpty(comment.ReplyToUserId))
                    {
                        comment.ReplyToUserId = parentComment.UserId;
                    }

                    if (string.IsNullOrEmpty(comment.ReplyToContent))
                    {
                        comment.ReplyToContent = parentComment.Content;
                    }

                    System.Diagnostics.Debug.WriteLine($"Populated reply info locally for comment {comment.Id}:");
                    System.Diagnostics.Debug.WriteLine($"  - ReplyToUserName: {comment.ReplyToUserName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Parent comment {comment.ParentCommentId} not found for comment {comment.Id}");
                }
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

                // Lấy thông tin user hiện tại để hiển thị ngay lập tức
                var currentUserDisplayName = GetCurrentUserDisplayName();

                var request = new CreateCommentRequest
                {
                    ActivityId = Activity.Id,
                    UserId = currentUserId,
                    Content = NewCommentText.Trim(),
                    ParentCommentId = ReplyingToComment?.Id
                };

                // Log debug chi tiết
                System.Diagnostics.Debug.WriteLine($"=== POST COMMENT DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"ActivityId: {request.ActivityId}");
                System.Diagnostics.Debug.WriteLine($"UserId: {request.UserId}");
                System.Diagnostics.Debug.WriteLine($"Content: {request.Content}");
                System.Diagnostics.Debug.WriteLine($"ParentCommentId: {request.ParentCommentId ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"Current User Display Name: {currentUserDisplayName}");

                var newComment = await _commentService.CreateCommentAsync(request, _cts.Token);

                if (newComment != null)
                {
                    System.Diagnostics.Debug.WriteLine($"=== NEW COMMENT CREATED ===");
                    System.Diagnostics.Debug.WriteLine($"Comment ID: {newComment.Id}");

                    // Đảm bảo thông tin user được set ngay lập tức
                    newComment.UserDisplayName = currentUserDisplayName;
                    newComment.UserId = currentUserId;
                    newComment.IsOwner = true;

                    // Nếu là phản hồi, đảm bảo thông tin reply được set đúng ngay lập tức
                    if (ReplyingToComment != null)
                    {
                        newComment.ReplyToUserName = ReplyingToComment.UserDisplayName;
                        newComment.ReplyToUserId = ReplyingToComment.UserId;
                        newComment.ReplyToContent = ReplyingToComment.Content;
                        newComment.ParentCommentId = ReplyingToComment.Id;

                        System.Diagnostics.Debug.WriteLine($"Reply info set immediately - To: {newComment.ReplyToUserName}");
                    }

                    // Đảm bảo avatar được set đúng
                    if (string.IsNullOrEmpty(newComment.UserAvatar))
                    {
                        newComment.UserAvatar = CurrentUserAvatar;
                    }

                    // Set thời gian nếu thiếu
                    if (newComment.CreatedAt == default(DateTime))
                    {
                        newComment.CreatedAt = DateTime.Now;
                    }

                    // Thêm comment vào đúng vị trí trong danh sách
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (string.IsNullOrEmpty(newComment.ParentCommentId))
                        {
                            // Comment gốc - thêm vào cuối
                            Comments.Add(newComment);
                            System.Diagnostics.Debug.WriteLine("Added as root comment");
                        }
                        else
                        {
                            // Reply - tìm vị trí đúng để insert
                            var insertIndex = FindReplyInsertIndex(newComment.ParentCommentId);
                            if (insertIndex >= 0 && insertIndex <= Comments.Count)
                            {
                                Comments.Insert(insertIndex, newComment);
                                System.Diagnostics.Debug.WriteLine($"Inserted reply at index: {insertIndex}");
                            }
                            else
                            {
                                Comments.Add(newComment);
                                System.Diagnostics.Debug.WriteLine("Added reply at end (fallback)");
                            }
                        }

                        // Trigger UI update
                        OnPropertyChanged(nameof(Comments));
                    });

                    // Clear input và reply state
                    NewCommentText = string.Empty;
                    var oldReplyingTo = ReplyingToComment;
                    ReplyingToComment = null;

                    var successMessage = request.ParentCommentId != null ?
                        $"Phản hồi đến {oldReplyingTo?.UserDisplayName} đã được gửi thành công!" :
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
                System.Diagnostics.Debug.WriteLine($"=== ERROR IN POST COMMENT ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");

                ErrorMessage = $"Lỗi khi đăng bình luận: {ex.Message}";
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi đăng bình luận",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetCurrentUserDisplayName()
        {
            try
            {
                // Thử lấy từ UserService trước
                var displayName = _userService?.GetCurrentUserDisplayName();
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }

                // Thử lấy từ App properties
                if (Application.Current?.Properties.Contains("CurrentUser") == true &&
                    Application.Current.Properties["CurrentUser"] is User currentUser &&
                    !string.IsNullOrEmpty(currentUser.DisplayName))
                {
                    return currentUser.DisplayName;
                }

                // Thử lấy từ Settings (nếu có lưu)
                var settingsDisplayName = DoanKhoaClient.Properties.Settings.Default.CurrentUserDisplayName;
                if (!string.IsNullOrEmpty(settingsDisplayName))
                {
                    return settingsDisplayName;
                }

                // Fallback
                return "Người dùng hiện tại";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user display name: {ex.Message}");
                return "Người dùng hiện tại";
            }
        }

        // Cập nhật phương thức này để tìm vị trí insert cho reply đệ quy
        private int FindReplyInsertIndex(string parentCommentId)
        {
            if (string.IsNullOrEmpty(parentCommentId))
                return Comments.Count;

            // Tìm comment cha và tất cả các replies trong thread đó
            var parentIndex = -1;
            var lastReplyInThreadIndex = -1;

            for (int i = 0; i < Comments.Count; i++)
            {
                if (Comments[i].Id == parentCommentId)
                {
                    parentIndex = i;
                    lastReplyInThreadIndex = i; // Bắt đầu từ comment cha
                }
                else if (parentIndex >= 0 && !string.IsNullOrEmpty(Comments[i].ParentCommentId))
                {
                    // Kiểm tra xem comment này có phải là reply trong cùng thread không
                    if (IsInSameThread(Comments[i].ParentCommentId, parentCommentId))
                    {
                        lastReplyInThreadIndex = i;
                    }
                    else if (string.IsNullOrEmpty(Comments[i].ParentCommentId))
                    {
                        // Gặp comment gốc mới, dừng tìm kiếm
                        break;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Parent index: {parentIndex}, Last reply in thread index: {lastReplyInThreadIndex}");

            if (lastReplyInThreadIndex >= 0)
            {
                // Thêm sau reply cuối cùng trong thread
                return lastReplyInThreadIndex + 1;
            }
            else
            {
                // Không tìm thấy, thêm vào cuối
                return Comments.Count;
            }
        }

        // Phương thức helper để kiểm tra xem comment có trong cùng thread không
        private bool IsInSameThread(string commentParentId, string targetParentId)
        {
            if (commentParentId == targetParentId)
                return true;

            // Tìm comment có ID = commentParentId
            var comment = Comments.FirstOrDefault(c => c.Id == commentParentId);
            if (comment != null && !string.IsNullOrEmpty(comment.ParentCommentId))
            {
                // Đệ quy kiểm tra parent của parent
                return IsInSameThread(comment.ParentCommentId, targetParentId);
            }

            return false;
        }

        private bool CanExecuteReplyToComment(object parameter)
        {
            // Cho phép reply trên tất cả comments (bao gồm cả replies)
            return !IsLoading && parameter is Comment;
        }

        private void ExecuteReplyToComment(object parameter)
        {
            if (parameter is Comment comment)
            {
                ReplyingToComment = comment;

                // Focus on comment input (this would need to be handled in the view)
                // Trigger property changed để UI cập nhật
                OnPropertyChanged(nameof(ReplyingToComment));
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

                // Tìm tất cả comments con (replies) của comment này trước khi xóa
                var commentToDelete = comment;
                var commentsToRemove = new List<Comment>();

                // Thêm comment gốc vào danh sách xóa
                commentsToRemove.Add(commentToDelete);

                // Tìm tất cả replies của comment này (đệ quy)
                FindAllRepliesRecursively(commentToDelete.Id, commentsToRemove);

                System.Diagnostics.Debug.WriteLine($"=== DELETING COMMENT AND REPLIES ===");
                System.Diagnostics.Debug.WriteLine($"Deleting comment: {commentToDelete.Id}");
                System.Diagnostics.Debug.WriteLine($"Total comments to remove: {commentsToRemove.Count}");

                // Xóa trên server
                var deleteSuccess = await _commentService.DeleteCommentAsync(commentToDelete.Id, _cts.Token);

                if (deleteSuccess)
                {
                    // Xóa khỏi UI ngay lập tức (bao gồm tất cả replies)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var commentToRemove in commentsToRemove)
                        {
                            Comments.Remove(commentToRemove);
                            System.Diagnostics.Debug.WriteLine($"Removed comment from UI: {commentToRemove.Id}");
                        }

                        // Trigger UI update
                        OnPropertyChanged(nameof(Comments));
                    });

                    MessageBox.Show($"Đã xóa bình luận và {commentsToRemove.Count - 1} phản hồi!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể xóa bình luận. Vui lòng thử lại.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private void FindAllRepliesRecursively(string parentCommentId, List<Comment> commentsToRemove)
        {
            var directReplies = Comments.Where(c => c.ParentCommentId == parentCommentId).ToList();

            foreach (var reply in directReplies)
            {
                // Thêm reply vào danh sách xóa
                commentsToRemove.Add(reply);
                System.Diagnostics.Debug.WriteLine($"Found reply to delete: {reply.Id}");

                // Tìm replies của reply này (đệ quy)
                FindAllRepliesRecursively(reply.Id, commentsToRemove);
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