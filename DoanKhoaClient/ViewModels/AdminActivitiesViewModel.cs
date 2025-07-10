using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Views;
using DoanKhoaClient.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Linq;

namespace DoanKhoaClient.ViewModels
{
    public class AdminActivitiesViewModel : INotifyPropertyChanged
    {
        private readonly ActivityService _activityService;
        private readonly CommentService _commentService;
        private ObservableCollection<Activity> _activities;
        private Activity _selectedActivity;
        private bool _isLoading;
        private string _errorMessage;
        private int _commentsCount90Days;

        // Thuộc tính mới cho chọn hàng loạt
        private bool _isAllSelected;
        private bool _hasSelectedItems;

        public ObservableCollection<Activity> Activities
        {
            get => _activities;
            set
            {
                _activities = value;
                OnPropertyChanged();
            }
        }

        public Activity SelectedActivity
        {
            get => _selectedActivity;
            set
            {
                _selectedActivity = value;
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
                _isAllSelected = value;
                OnPropertyChanged();

                if (Activities != null)
                {
                    foreach (var activity in Activities)
                    {
                        activity.IsSelected = value;
                    }
                }

                UpdateHasSelectedItems();
            }
        }

        public bool HasSelectedItems
        {
            get => _hasSelectedItems;
            set
            {
                if (_hasSelectedItems != value)
                {
                    _hasSelectedItems = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int CommentsCount90Days
        {
            get => _commentsCount90Days;
            set
            {
                _commentsCount90Days = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand CreateActivityCommand { get; }
        public ICommand EditActivityCommand { get; }
        public ICommand DeleteActivityCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand BatchEditCommand { get; }
        public ICommand BatchDeleteCommand { get; }
        public ICommand ViewDetailCommand { get; }

        public ObservableCollection<FilterOption> ActivityTypeOptions { get; set; }
        public ObservableCollection<FilterOption> ActivityStatusOptions { get; set; }
        public ObservableCollection<string> SelectedFilterTags { get; set; } = new ObservableCollection<string>();
        private bool _isFilterDropdownOpen;
        public bool IsFilterDropdownOpen
        {
            get => _isFilterDropdownOpen;
            set { _isFilterDropdownOpen = value; OnPropertyChanged(); }
        }
        public ICommand RemoveFilterTagCommand { get; }
        public ICommand ApplyFilterCommand { get; }

        // Thống kê 90 ngày qua
        public int ActivitiesCount90Days => Activities?.Count(a => a.Date >= DateTime.Now.AddDays(-90)) ?? 0;
        public int ParticipantsCount90Days => Activities?.Where(a => a.Date >= DateTime.Now.AddDays(-90)).Sum(a => a.ParticipantCount) ?? 0;
        public int LikesCount90Days => Activities?.Where(a => a.Date >= DateTime.Now.AddDays(-90)).Sum(a => a.LikeCount) ?? 0;

        public AdminActivitiesViewModel(ActivityService activityService = null, CommentService commentService = null)
        {
            _activityService = activityService ?? new ActivityService();
            _commentService = commentService ?? new CommentService();
            Activities = new ObservableCollection<Activity>();

            // Initialize commands
            CreateActivityCommand = new RelayCommand(async param => await ExecuteCreateActivityAsync(),
                param => !IsLoading);

            EditActivityCommand = new RelayCommand(async param => await ExecuteEditActivityAsync(param as Activity),
                param => !IsLoading && param is Activity);

            DeleteActivityCommand = new RelayCommand(async param => await ExecuteDeleteActivityAsync(param as Activity),
                param => !IsLoading && param is Activity);

            RefreshCommand = new RelayCommand(async _ => await LoadActivitiesAsync(),
                _ => !IsLoading);

            BatchEditCommand = new RelayCommand(param => ExecuteBatchEdit(),
                param => HasSelectedItems && !IsLoading);

            BatchDeleteCommand = new RelayCommand(async param => await ExecuteBatchDeleteAsync(),
                param => HasSelectedItems && !IsLoading);

            ViewDetailCommand = new RelayCommand(param => ExecuteViewDetail(param as Activity),
                param => !IsLoading && param is Activity);

            // Initialize filter options
            ActivityTypeOptions = new ObservableCollection<FilterOption>
            {
                new FilterOption { Display = "Hoạt động học thuật", Value = ActivityType.Academic },
                new FilterOption { Display = "Hoạt động tình nguyện", Value = ActivityType.Volunteer },
                new FilterOption { Display = "Hoạt động ngoại khóa", Value = ActivityType.Entertainment }
            };

            ActivityStatusOptions = new ObservableCollection<FilterOption>
            {
                new FilterOption { Display = "Sắp diễn ra", Value = ActivityStatus.Upcoming },
                new FilterOption { Display = "Đang diễn ra", Value = ActivityStatus.Ongoing },
                new FilterOption { Display = "Đã diễn ra", Value = ActivityStatus.Completed }
            };

            RemoveFilterTagCommand = new RelayCommand(tag =>
            {
                foreach (var opt in ActivityTypeOptions.Concat(ActivityStatusOptions))
                    if (opt.Display == (string)tag) opt.IsChecked = false;
                UpdateSelectedTags();
                FilterActivities();
            });

            ApplyFilterCommand = new RelayCommand(_ =>
            {
                UpdateSelectedTags();
                FilterActivities();
                IsFilterDropdownOpen = false;
            });

            // Load data
            _ = LoadActivitiesAsync();
        }


        private async Task LoadActivitiesAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var activities = await _activityService.GetActivitiesAsync();

                // Load comment count for each activity
                foreach (var activity in activities)
                {
                    try
                    {
                        var comments = await _commentService.GetCommentsByActivityIdAsync(activity.Id);
                        activity.CommentCount = comments?.Count ?? 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading comments for activity {activity.Id}: {ex.Message}");
                        activity.CommentCount = 0;
                    }
                }

                // Sort activities by date
                var sortedActivities = activities.OrderByDescending(a => a.Date).ToList();

                Activities = new ObservableCollection<Activity>(sortedActivities);
                UpdateHasSelectedItems();
                UpdateIsAllSelected();

                // Update statistics
                OnPropertyChanged(nameof(ActivitiesCount90Days));
                OnPropertyChanged(nameof(ParticipantsCount90Days));
                OnPropertyChanged(nameof(LikesCount90Days));

                // Load comments count for 90 days
                await LoadCommentsCount90DaysAsync();

            }, "Không thể tải danh sách hoạt động");
        }

        private async Task LoadCommentsCount90DaysAsync()
        {
            try
            {
                var date90DaysAgo = DateTime.Now.AddDays(-90);
                var totalCommentsCount = 0;

                // Get activities from 90 days ago
                var activities90Days = Activities?.Where(a => a.Date >= date90DaysAgo).ToList() ?? new List<Activity>();

                foreach (var activity in activities90Days)
                {
                    try
                    {
                        // Get comments for each activity
                        var comments = await _commentService.GetCommentsByActivityIdAsync(activity.Id);
                        if (comments != null)
                        {
                            // Count comments created in the last 90 days
                            var commentsIn90Days = comments.Count(c => c.CreatedAt >= date90DaysAgo);
                            totalCommentsCount += commentsIn90Days;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading comments for activity {activity.Id}: {ex.Message}");
                    }
                }

                CommentsCount90Days = totalCommentsCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating comments count: {ex.Message}");
                CommentsCount90Days = 0;
            }
        }

        private async Task ExecuteCreateActivityAsync()
        {
            var createDialog = new CreateActivityDialog();
            if (createDialog.ShowDialog() == true)
            {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    createDialog.Activity.Id = null;
                    var newActivity = await _activityService.CreateActivityAsync(createDialog.Activity);

                    // Initialize comment count for new activity
                    newActivity.CommentCount = 0;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Activities.Add(newActivity);
                        Activities = new ObservableCollection<Activity>(
                            Activities.OrderByDescending(a => a.Date)
                        );
                        ShowSuccessMessage("Tạo hoạt động thành công!");
                    });
                }, "Không thể tạo hoạt động");
            }
        }

        private async Task ExecuteEditActivityAsync(Activity activity)
        {
            if (activity == null) return;

            var activityToEdit = CloneActivity(activity);
            var editDialog = new EditActivityDialog(activityToEdit);

            if (editDialog.ShowDialog() == true)
            {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    var updatedActivity = await _activityService.UpdateActivityAsync(
                        activity.Id, editDialog.Activity);

                    // Preserve comment count
                    updatedActivity.CommentCount = activity.CommentCount;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var index = Activities.IndexOf(activity);
                        if (index >= 0)
                        {
                            Activities[index] = updatedActivity;
                            Activities = new ObservableCollection<Activity>(
                                Activities.OrderByDescending(a => a.Date)
                            );
                        }
                        ShowSuccessMessage("Cập nhật hoạt động thành công!");
                    });
                }, "Không thể cập nhật hoạt động");
            }
        }

        private async Task ExecuteDeleteActivityAsync(Activity activity)
        {
            if (activity == null) return;

            if (MessageBox.Show("Bạn có chắc muốn xoá hoạt động này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    await _activityService.DeleteActivityAsync(activity.Id);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Activities.Remove(activity);
                        UpdateHasSelectedItems();
                        ShowSuccessMessage("Xóa hoạt động thành công!");
                    });
                }, "Không thể xóa hoạt động");
            }
        }

        private void ExecuteBatchEdit()
        {
            var selectedActivities = Activities.Where(a => a.IsSelected).ToList();
            if (!selectedActivities.Any()) return;

            var batchEditDialog = new BatchEditDialog(selectedActivities);
            if (batchEditDialog.ShowDialog() == true)
            {
                var updatedFields = batchEditDialog.UpdatedFields;

                foreach (var activity in selectedActivities)
                {
                    if (updatedFields.ContainsKey("Type") && updatedFields["Type"] != null)
                    {
                        activity.Type = (ActivityType)updatedFields["Type"];
                    }

                    if (updatedFields.ContainsKey("Status") && updatedFields["Status"] != null)
                    {
                        activity.Status = (ActivityStatus)updatedFields["Status"];
                    }

                    if (updatedFields.ContainsKey("Date") && updatedFields["Date"] != null)
                    {
                        activity.Date = (DateTime)updatedFields["Date"];
                    }

                    _activityService.UpdateActivityAsync(activity.Id, activity);
                }

                Activities = new ObservableCollection<Activity>(
                    Activities.OrderByDescending(a => a.Date)
                );

                ShowSuccessMessage("Cập nhật hàng loạt thành công!");
            }
        }

        private async Task ExecuteBatchDeleteAsync()
        {
            var selectedActivities = Activities.Where(a => a.IsSelected).ToList();
            if (!selectedActivities.Any()) return;

            if (MessageBox.Show($"Bạn có chắc muốn xoá {selectedActivities.Count} hoạt động đã chọn?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    foreach (var activity in selectedActivities)
                    {
                        await _activityService.DeleteActivityAsync(activity.Id);
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var activity in selectedActivities.ToList())
                        {
                            Activities.Remove(activity);
                        }

                        UpdateHasSelectedItems();
                        ShowSuccessMessage("Xóa hàng loạt thành công!");
                    });
                }, "Không thể xóa một số hoạt động");
            }
        }

        private void ExecuteViewDetail(Activity activity)
        {
            if (activity == null) return;

            // Mở màn hình chi tiết hoạt động
            var detailView = new AdminActivityDetailView(activity);
            detailView.Show();
        }

        public void UpdateHasSelectedItems()
        {
            HasSelectedItems = Activities != null && Activities.Any(a => a.IsSelected);
        }

        public void UpdateIsAllSelected()
        {
            bool allSelected = Activities != null && Activities.Count > 0 && Activities.All(a => a.IsSelected);
            if (_isAllSelected != allSelected)
            {
                _isAllSelected = allSelected;
                OnPropertyChanged(nameof(IsAllSelected));
            }
        }

        private async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string defaultErrorMessage)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await action();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorMessage = $"{defaultErrorMessage}: {ex.Message}";
                    MessageBox.Show(ErrorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Activity CloneActivity(Activity source)
        {
            return new Activity
            {
                Id = source.Id,
                Title = source.Title,
                Description = source.Description,
                Type = source.Type,
                Date = source.Date,
                ImgUrl = source.ImgUrl,
                CreatedAt = source.CreatedAt,
                Status = source.Status,
                ParticipantCount = source.ParticipantCount,
                LikeCount = source.LikeCount,
                CommentCount = source.CommentCount
            };
        }

        private void UpdateSelectedTags()
        {
            SelectedFilterTags.Clear();
            foreach (var opt in ActivityTypeOptions.Where(o => o.IsChecked))
                SelectedFilterTags.Add(opt.Display);
            foreach (var opt in ActivityStatusOptions.Where(o => o.IsChecked))
                SelectedFilterTags.Add(opt.Display);
        }

        private void FilterActivities()
        {
            var selectedTypes = ActivityTypeOptions.Where(o => o.IsChecked).Select(o => (ActivityType)o.Value).ToList();
            var selectedStatuses = ActivityStatusOptions.Where(o => o.IsChecked).Select(o => (ActivityStatus)o.Value).ToList();

            var filtered = Activities.Where(a =>
                (selectedTypes.Count == 0 || selectedTypes.Contains(a.Type)) &&
                (selectedStatuses.Count == 0 || selectedStatuses.Contains(a.Status))
            ).ToList();

            Activities = new ObservableCollection<Activity>(filtered);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class FilterOption : INotifyPropertyChanged
    {
        public string Display { get; set; }
        public object Value { get; set; }
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}