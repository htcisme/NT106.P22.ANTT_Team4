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
        private ObservableCollection<Activity> _activities;
        private Activity _selectedActivity;
        private bool _isLoading;
        private string _errorMessage;

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
                // Đảm bảo các command được cập nhật trạng thái khi IsLoading thay đổi
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

        // Thuộc tính mới cho chọn hàng loạt
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                _isAllSelected = value;
                OnPropertyChanged();

                // Cập nhật trạng thái IsSelected của tất cả các mục
                if (Activities != null)
                {
                    foreach (var activity in Activities)
                    {
                        activity.IsSelected = value;
                    }
                }

                // Cập nhật trạng thái của HasSelectedItems
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

        // Command hiện có
        public ICommand CreateActivityCommand { get; }
        public ICommand EditActivityCommand { get; }
        public ICommand DeleteActivityCommand { get; }
        public ICommand RefreshCommand { get; }

        // Command mới cho thao tác hàng loạt
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

        // Thống kê 50 ngày qua
        public int ActivitiesCount50Days => Activities?.Count(a => a.Date >= DateTime.Now.AddDays(-50)) ?? 0;
        public int ParticipantsCount50Days => Activities?.Where(a => a.Date >= DateTime.Now.AddDays(-50)).Sum(a => a.ParticipantCount) ?? 0;
        public int LikesCount50Days => Activities?.Where(a => a.Date >= DateTime.Now.AddDays(-50)).Sum(a => a.LikeCount) ?? 0;

        public AdminActivitiesViewModel(ActivityService activityService = null)
        {
            _activityService = activityService ?? new ActivityService();
            Activities = new ObservableCollection<Activity>();

            // Command hiện có
            CreateActivityCommand = new RelayCommand(async param => await ExecuteCreateActivityAsync(),
                param => !IsLoading);

            EditActivityCommand = new RelayCommand(async param => await ExecuteEditActivityAsync(param as Activity),
                param => !IsLoading && param is Activity);

            DeleteActivityCommand = new RelayCommand(async param => await ExecuteDeleteActivityAsync(param as Activity),
                param => !IsLoading && param is Activity);

            RefreshCommand = new RelayCommand(async _ => await LoadActivitiesAsync(),
                _ => !IsLoading);

            // Command mới cho thao tác hàng loạt
            BatchEditCommand = new RelayCommand(param => ExecuteBatchEdit(),
                param => HasSelectedItems && !IsLoading);

            BatchDeleteCommand = new RelayCommand(async param => await ExecuteBatchDeleteAsync(),
                param => HasSelectedItems && !IsLoading);

            ViewDetailCommand = new RelayCommand(param => ExecuteViewDetail(param as Activity),
                param => !IsLoading && param is Activity);

            // Fire and forget pattern - không chờ đợi
            _ = LoadActivitiesAsync();

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
        }

        private async Task LoadActivitiesAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var activities = await _activityService.GetActivitiesAsync();

                // Sắp xếp hoạt động theo thời gian gần nhất
                var sortedActivities = activities
                    .OrderByDescending(a => a.Date)
                    .ToList();

                Activities = new ObservableCollection<Activity>(sortedActivities);
                UpdateHasSelectedItems();
                UpdateIsAllSelected();
                // Notify các property thống kê
                OnPropertyChanged(nameof(ActivitiesCount50Days));
                OnPropertyChanged(nameof(ParticipantsCount50Days));
                OnPropertyChanged(nameof(LikesCount50Days));
            }, "Không thể tải danh sách hoạt động");
        }

        private bool CanExecuteEditDelete()
        {
            return SelectedActivity != null && !IsLoading;
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

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Activities.Add(newActivity);
                        // Sắp xếp lại collection sau khi thêm
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

            // Tạo bản sao để tránh thay đổi trực tiếp đối tượng gốc khi chưa lưu
            var activityToEdit = CloneActivity(activity);
            var editDialog = new EditActivityDialog(activityToEdit);

            if (editDialog.ShowDialog() == true)
            {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    var updatedActivity = await _activityService.UpdateActivityAsync(
                        activity.Id, editDialog.Activity);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var index = Activities.IndexOf(activity);
                        if (index >= 0)
                        {
                            Activities[index] = updatedActivity;
                            // Sắp xếp lại collection sau khi cập nhật
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
                    var idToDelete = activity.Id;
                    await _activityService.DeleteActivityAsync(idToDelete);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Activities.Remove(activity);
                        UpdateHasSelectedItems();
                        ShowSuccessMessage("Xóa hoạt động thành công!");
                    });
                }, "Không thể xóa hoạt động");
            }
        }

        // Phương thức mới cho thao tác hàng loạt
        private void ExecuteBatchEdit()
        {
            var selectedActivities = Activities.Where(a => a.IsSelected).ToList();
            if (!selectedActivities.Any()) return;

            // Hiển thị màn hình chỉnh sửa hàng loạt
            var batchEditDialog = new BatchEditDialog(selectedActivities);
            if (batchEditDialog.ShowDialog() == true)
            {
                // Áp dụng các thay đổi hàng loạt
                var updatedFields = batchEditDialog.UpdatedFields;

                foreach (var activity in selectedActivities)
                {
                    // Áp dụng các thay đổi tùy theo các trường được chọn để cập nhật
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

                    // Cập nhật hoạt động thông qua service
                    _activityService.UpdateActivityAsync(activity.Id, activity);
                }

                // Sắp xếp lại danh sách
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
            var detailView = new AdminActivitiesPostView(activity);
            detailView.Show();
        }

        // Phương thức để cập nhật trạng thái HasSelectedItems
        public void UpdateHasSelectedItems()
        {
            HasSelectedItems = Activities != null && Activities.Any(a => a.IsSelected);
        }

        // Phương thức để cập nhật trạng thái IsAllSelected dựa trên các mục đã chọn
        public void UpdateIsAllSelected()
        {
            bool allSelected = Activities != null && Activities.Count > 0 && Activities.All(a => a.IsSelected);
            if (_isAllSelected != allSelected)
            {
                _isAllSelected = allSelected;
                OnPropertyChanged(nameof(IsAllSelected));
            }
        }

        // Helper method để xử lý lỗi một cách nhất quán
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

        // Helper method để hiển thị thông báo thành công
        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Helper method để tạo bản sao của Activity
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
                Status = source.Status
            };
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
                // ... các điều kiện khác như search text ...
            ).ToList();

            Activities = new ObservableCollection<Activity>(filtered);
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