using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Linq;
using DoanKhoaClient.Views;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;

namespace DoanKhoaClient.ViewModels
{
    public class ActivitiesViewModel : INotifyPropertyChanged
    {
        private readonly ActivityService _activityService;
        private ObservableCollection<Activity> _activities;
        private ObservableCollection<Activity> _latestActivities;
        private ObservableCollection<FeaturedActivity> _featuredActivities;
        private ObservableCollection<PaginationDot> _paginationDots;
        private bool _isLoading;
        private string _errorMessage;
        private Activity _selectedActivity;
        private DispatcherTimer _carouselTimer;
        private int _currentFeaturedIndex = 0;
        private string _searchText;
        private ActivityType? _selectedType;
        private ActivityStatus? _selectedStatus;
        private DateTime? _dateFrom;
        private DateTime? _dateTo;
        private ObservableCollection<Activity> _filteredActivities = new ObservableCollection<Activity>();
        private ObservableCollection<SearchField> _searchFields;
        private SearchField _selectedSearchField;
        private ObservableCollection<NullableTypeItem> _typeFilterOptions;
        private ObservableCollection<NullableStatusItem> _statusFilterOptions;
        private NullableTypeItem _selectedTypeItem;
        private NullableStatusItem _selectedStatusItem;
        private bool _isSearchResultOpen;

        // Thêm thuộc tính cho Filter Dropdown
        private bool _isFilterDropdownOpen;
        private ObservableCollection<FilterOption<ActivityType>> _activityTypeOptions;
        private ObservableCollection<FilterOption<ActivityStatus>> _activityStatusOptions;
        private ObservableCollection<string> _selectedFilterTags;
        private bool _isSearchDropdownOpen;


    
        public ObservableCollection<Activity> GetFilteredActivities(string keyword)
        {
            // Giả sử bạn có danh sách gốc là AllActivities
            return new ObservableCollection<Activity>(
                Activities.Where(a => a.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            );
        }

       


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

        public ObservableCollection<Activity> LatestActivities
        {
            get => _latestActivities;
            set { _latestActivities = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FeaturedActivity> FeaturedActivities
        {
            get => _featuredActivities;
            set { _featuredActivities = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PaginationDot> PaginationDots
        {
            get => _paginationDots;
            set { _paginationDots = value; OnPropertyChanged(); }
        }

        public Activity SelectedActivity
        {
            get => _selectedActivity;
            set { _selectedActivity = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterActivities(); }
        }

        public ActivityType? SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(); FilterActivities(); }
        }

        public ActivityStatus? SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); FilterActivities(); }
        }

        public DateTime? DateFrom
        {
            get => _dateFrom;
            set { _dateFrom = value; OnPropertyChanged(); FilterActivities(); }
        }

        public DateTime? DateTo
        {
            get => _dateTo;
            set { _dateTo = value; OnPropertyChanged(); FilterActivities(); }
        }

        public ObservableCollection<Activity> FilteredActivities
        {
            get => _filteredActivities;
            set { _filteredActivities = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SearchField> SearchFields
        {
            get => _searchFields;
            set { _searchFields = value; OnPropertyChanged(); }
        }

        public SearchField SelectedSearchField
        {
            get => _selectedSearchField;
            set { _selectedSearchField = value; OnPropertyChanged(); FilterActivities(); }
        }

        public ObservableCollection<NullableTypeItem> TypeFilterOptions
        {
            get => _typeFilterOptions;
            set { _typeFilterOptions = value; OnPropertyChanged(); }
        }

        public ObservableCollection<NullableStatusItem> StatusFilterOptions
        {
            get => _statusFilterOptions;
            set { _statusFilterOptions = value; OnPropertyChanged(); }
        }

        public NullableTypeItem SelectedTypeItem
        {
            get => _selectedTypeItem;
            set { _selectedTypeItem = value; OnPropertyChanged(); FilterActivities(); }
        }

        public NullableStatusItem SelectedStatusItem
        {
            get => _selectedStatusItem;
            set { _selectedStatusItem = value; OnPropertyChanged(); FilterActivities(); }
        }

        public bool IsSearchResultOpen
        {
            get => _isSearchResultOpen;
            set { _isSearchResultOpen = value; OnPropertyChanged(); }
        }

        // Thuộc tính mới cho Filter Dropdown
        public bool IsFilterDropdownOpen
        {
            get => _isFilterDropdownOpen;
            set { _isFilterDropdownOpen = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FilterOption<ActivityType>> ActivityTypeOptions
        {
            get => _activityTypeOptions;
            set { _activityTypeOptions = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FilterOption<ActivityStatus>> ActivityStatusOptions
        {
            get => _activityStatusOptions;
            set { _activityStatusOptions = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> SelectedFilterTags
        {
            get => _selectedFilterTags;
            set { _selectedFilterTags = value; OnPropertyChanged(); }
        }

        public ICommand CreateActivityCommand { get; }
        public ICommand EditActivityCommand { get; }
        public ICommand DeleteActivityCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenActivityDetailCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand RemoveFilterTagCommand { get; }

        public ActivitiesViewModel(ActivityService activityService = null)
        {
            _activityService = activityService ?? new ActivityService();
            Activities = new ObservableCollection<Activity>();
            LatestActivities = new ObservableCollection<Activity>();
            InitializeFeaturedActivities();
            InitializePaginationDots();
            InitializeCarouselTimer();

            // Khởi tạo các trường tìm kiếm
            SearchFields = new ObservableCollection<SearchField>
            {
                new SearchField { DisplayName = "Tất cả", Field = "All" },
                new SearchField { DisplayName = "Tiêu đề", Field = "Title" },
                new SearchField { DisplayName = "Mô tả", Field = "Description" },
                new SearchField { DisplayName = "Loại", Field = "Type" },
                new SearchField { DisplayName = "Trạng thái", Field = "Status" }
            };
            SelectedSearchField = SearchFields[0];

            // Khởi tạo Type/Status filter có null
            TypeFilterOptions = new ObservableCollection<NullableTypeItem>
            {
                new NullableTypeItem { Display = "Tất cả", Value = null },
                new NullableTypeItem { Display = "Academic", Value = ActivityType.Academic },
                new NullableTypeItem { Display = "Volunteer", Value = ActivityType.Volunteer },
                new NullableTypeItem { Display = "Entertainment", Value = ActivityType.Entertainment }
            };
            StatusFilterOptions = new ObservableCollection<NullableStatusItem>
            {
                new NullableStatusItem { Display = "Tất cả", Value = null },
                new NullableStatusItem { Display = "Upcoming", Value = ActivityStatus.Upcoming },
                new NullableStatusItem { Display = "Ongoing", Value = ActivityStatus.Ongoing },
                new NullableStatusItem { Display = "Completed", Value = ActivityStatus.Completed }
            };
            SelectedTypeItem = TypeFilterOptions[0];
            SelectedStatusItem = StatusFilterOptions[0];

            // Khởi tạo cho bộ lọc dropdown
            ActivityTypeOptions = new ObservableCollection<FilterOption<ActivityType>>
            {
                new FilterOption<ActivityType> { Display = "Academic", Value = ActivityType.Academic, IsChecked = false },
                new FilterOption<ActivityType> { Display = "Volunteer", Value = ActivityType.Volunteer, IsChecked = false },
                new FilterOption<ActivityType> { Display = "Entertainment", Value = ActivityType.Entertainment, IsChecked = false }
            };

            ActivityStatusOptions = new ObservableCollection<FilterOption<ActivityStatus>>
            {
                new FilterOption<ActivityStatus> { Display = "Upcoming", Value = ActivityStatus.Upcoming, IsChecked = false },
                new FilterOption<ActivityStatus> { Display = "Ongoing", Value = ActivityStatus.Ongoing, IsChecked = false },
                new FilterOption<ActivityStatus> { Display = "Completed", Value = ActivityStatus.Completed, IsChecked = false }
            };

            SelectedFilterTags = new ObservableCollection<string>();

            CreateActivityCommand = new RelayCommand(async _ => await ExecuteCreateActivityAsync(), _ => !IsLoading);
            EditActivityCommand = new RelayCommand(async param => await ExecuteEditActivityAsync(param as Activity), param => !IsLoading && param is Activity);
            DeleteActivityCommand = new RelayCommand(async param => await ExecuteDeleteActivityAsync(param as Activity), param => !IsLoading && param is Activity);
            RefreshCommand = new RelayCommand(async _ => await LoadActivitiesAsync(), _ => !IsLoading);
            OpenActivityDetailCommand = new RelayCommand(param => ExecuteOpenActivityDetail(param as Activity), param => param is Activity);
            ApplyFilterCommand = new RelayCommand(_ => ExecuteApplyFilter());
            RemoveFilterTagCommand = new RelayCommand(param => ExecuteRemoveFilterTag(param as string));

            _ = LoadActivitiesAsync();
        }

        private void ExecuteApplyFilter()
        {
            // Xóa tất cả các tag đã chọn trước đó
            SelectedFilterTags.Clear();

            // Thêm các tag mới dựa trên bộ lọc đã chọn
            foreach (var typeOption in ActivityTypeOptions.Where(o => o.IsChecked))
            {
                SelectedFilterTags.Add($"Loại: {typeOption.Display}");
            }

            foreach (var statusOption in ActivityStatusOptions.Where(o => o.IsChecked))
            {
                SelectedFilterTags.Add($"Trạng thái: {statusOption.Display}");
            }

            // Cập nhật lại bộ lọc
            FilterActivities();

            // Đóng popup
            IsFilterDropdownOpen = false;
        }

        private void ExecuteRemoveFilterTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            // Xóa tag khỏi danh sách đã chọn
            SelectedFilterTags.Remove(tag);

            // Cập nhật lại bộ lọc trong options
            if (tag.StartsWith("Loại:"))
            {
                string typeValue = tag.Substring(5).Trim();
                var typeOption = ActivityTypeOptions.FirstOrDefault(o => o.Display == typeValue);
                if (typeOption != null)
                {
                    typeOption.IsChecked = false;
                }
            }
            else if (tag.StartsWith("Trạng thái:"))
            {
                string statusValue = tag.Substring(10).Trim();
                var statusOption = ActivityStatusOptions.FirstOrDefault(o => o.Display == statusValue);
                if (statusOption != null)
                {
                    statusOption.IsChecked = false;
                }
            }

            // Cập nhật lại dữ liệu
            FilterActivities();
        }

        private void InitializeFeaturedActivities()
        {
            FeaturedActivities = new ObservableCollection<FeaturedActivity>
            {
                new FeaturedActivity
                {
                    Title = "VNU TOUR 2024",
                    ImgUrl = "/Views/Images/vnutour.png",
                    Width = 175,
                    Height = 175,
                    Opacity = 1.0,
                    IsActive = true
                },
                new FeaturedActivity
                {
                    Title = "NGỌN ĐUỐC XANH 2025",
                    ImgUrl = "/Views/Images/ndx.png",
                    Width = 175,
                    Height = 175,
                    Opacity = 0.8,
                    IsActive = false
                },
                new FeaturedActivity
                {
                    Title = "NETSEC DAY 2024",
                    ImgUrl = "/Views/Images/netsec.png",
                    Width = 175,
                    Height = 175,
                    Opacity = 0.8,
                    IsActive = false
                }
            };
        }

        private void InitializePaginationDots()
        {
            PaginationDots = new ObservableCollection<PaginationDot>
            {
                new PaginationDot
                {
                    Color = new SolidColorBrush(Color.FromRgb(89, 124, 162)),
                    IsActive = true
                },
                new PaginationDot
                {
                    Color = new SolidColorBrush(Color.FromRgb(219, 236, 247)),
                    IsActive = false
                },
                new PaginationDot
                {
                    Color = new SolidColorBrush(Color.FromRgb(219, 236, 247)),
                    IsActive = false
                }
            };
        }

        private void InitializeCarouselTimer()
        {
            _carouselTimer = new DispatcherTimer();
            _carouselTimer.Interval = TimeSpan.FromSeconds(4);
            _carouselTimer.Tick += async (s, e) => await CarouselTransitionAsync();
            _carouselTimer.Start();
        }

        private async Task CarouselTransitionAsync()
        {
            if (FeaturedActivities.Count < 3) return;

            // Store current state
            var currentActiveIndex = FeaturedActivities.IndexOf(FeaturedActivities.FirstOrDefault(f => f.IsActive));
            var nextActiveIndex = (currentActiveIndex + 1) % FeaturedActivities.Count;

            // Prepare for transition
            for (int i = 0; i < FeaturedActivities.Count; i++)
            {
                if (i == currentActiveIndex)
                {
                    FeaturedActivities[i].IsActive = false;
                }
                else if (i == nextActiveIndex)
                {
                    FeaturedActivities[i].IsActive = true;
                }
            }

            // Update dots
            for (int i = 0; i < PaginationDots.Count; i++)
            {
                PaginationDots[i].IsActive = (i == nextActiveIndex);
                PaginationDots[i].Color = (i == nextActiveIndex)
                    ? new SolidColorBrush(Color.FromRgb(89, 124, 162))
                    : new SolidColorBrush(Color.FromRgb(219, 236, 247));
            }

            // Smooth transition animation
            for (double t = 0; t <= 1; t += 0.1)
            {
                // Calculate opacity for each item
                for (int i = 0; i < FeaturedActivities.Count; i++)
                {
                    if (i == currentActiveIndex)
                    {
                        FeaturedActivities[i].Opacity = 1 - t;
                    }
                    else if (i == nextActiveIndex)
                    {
                        FeaturedActivities[i].Opacity = t;
                    }
                    else
                    {
                        FeaturedActivities[i].Opacity = 0.8; // Keep other items visible but slightly dimmed
                    }
                }
                await Task.Delay(20);
            }

            // Ensure final state with clear images
            for (int i = 0; i < FeaturedActivities.Count; i++)
            {
                if (i == nextActiveIndex)
                {
                    FeaturedActivities[i].Opacity = 1.0;
                    FeaturedActivities[i].IsActive = true;
                }
                else
                {
                    FeaturedActivities[i].Opacity = 0.8;
                    FeaturedActivities[i].IsActive = false;
                }
            }
        }

        public async Task LoadActivitiesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                var activities = await _activityService.GetActivitiesAsync();
                var sorted = activities.OrderByDescending(a => a.Date).ToList();
                Activities = new ObservableCollection<Activity>(sorted);
                LatestActivities = new ObservableCollection<Activity>(sorted.Take(3));
                FilterActivities();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể tải danh sách hoạt động: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteCreateActivityAsync()
        {
            // TODO: Hiển thị dialog tạo mới, lấy dữ liệu và gọi API
            MessageBox.Show("Tính năng tạo mới hoạt động chưa được cài đặt.");
        }

        private async Task ExecuteEditActivityAsync(Activity activity)
        {
            if (activity == null) return;
            // TODO: Hiển thị dialog sửa, lấy dữ liệu và gọi API
            MessageBox.Show("Tính năng sửa hoạt động chưa được cài đặt.");
        }

        private async Task ExecuteDeleteActivityAsync(Activity activity)
        {
            if (activity == null) return;
            if (MessageBox.Show("Bạn có chắc muốn xoá hoạt động này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await _activityService.DeleteActivityAsync(activity.Id);
                    Activities.Remove(activity);
                    // Cập nhật lại LatestActivities khi xóa
                    LatestActivities = new ObservableCollection<Activity>(Activities.Take(3));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể xóa hoạt động: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecuteOpenActivityDetail(Activity activity)
        {
            if (activity == null) return;
            var postView = new ActivitiesPostView(activity);
            postView.Show();
            IsSearchResultOpen = false;
        }

        public void FilterActivities()
        {
            if (Activities == null) return;

            var filtered = Activities.AsEnumerable();

            // Lọc theo nội dung tìm kiếm
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(a =>
                    (SelectedSearchField?.Field == "All" && (
                        (a.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (a.Description?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        a.Type.ToString().IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        a.Status.ToString().IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0
                    )) ||
                    (SelectedSearchField?.Field == "Title" && (a.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    (SelectedSearchField?.Field == "Description" && (a.Description?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    (SelectedSearchField?.Field == "Type" && a.Type.ToString().IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (SelectedSearchField?.Field == "Status" && a.Status.ToString().IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            // Lọc theo Type từ dropdown filter
            if (ActivityTypeOptions != null)
            {
                var selectedTypes = ActivityTypeOptions
                    .Where(o => o != null && o.IsChecked)
                    .Select(o => o.Value)
                    .ToList();

                if (selectedTypes.Any())
                {
                    filtered = filtered.Where(a => selectedTypes.Contains(a.Type));
                }
            }

            // Lọc theo Status từ dropdown filter
            if (ActivityStatusOptions != null)
            {
                var selectedStatuses = ActivityStatusOptions
                    .Where(o => o != null && o.IsChecked)
                    .Select(o => o.Value)
                    .ToList();

                if (selectedStatuses.Any())
                {
                    filtered = filtered.Where(a => selectedStatuses.Contains(a.Status));
                }
            }

            // Lọc theo Type/Status từ combobox (nếu có)
            if (SelectedTypeItem?.Value.HasValue ?? false)
            {
                filtered = filtered.Where(a => a.Type == SelectedTypeItem.Value);
            }

            if (SelectedStatusItem?.Value.HasValue ?? false)
            {
                filtered = filtered.Where(a => a.Status == SelectedStatusItem.Value);
            }

            // Lọc theo ngày tháng
            if (DateFrom.HasValue)
            {
                filtered = filtered.Where(a => a.Date >= DateFrom.Value);
            }

            if (DateTo.HasValue)
            {
                filtered = filtered.Where(a => a.Date <= DateTo.Value);
            }

            FilteredActivities = new ObservableCollection<Activity>(filtered);
            IsSearchResultOpen = !string.IsNullOrWhiteSpace(SearchText) && FilteredActivities.Any();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class FeaturedActivity : INotifyPropertyChanged
    {
        private double _opacity = 1.0;
        private bool _isActive = false;

        public string Title { get; set; }
        public string ImgUrl { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double Opacity
        {
            get => _opacity;
            set
            {
                _opacity = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class PaginationDot : INotifyPropertyChanged
    {
        private bool _isActive;
        private SolidColorBrush _color;
        public SolidColorBrush Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SearchField
    {
        public string DisplayName { get; set; }
        public string Field { get; set; }
    }

    public class NullableTypeItem
    {
        public string Display { get; set; }
        public ActivityType? Value { get; set; }
    }

    public class NullableStatusItem
    {
        public string Display { get; set; }
        public ActivityStatus? Value { get; set; }
    }

    public class FilterOption<T>
    {
        private bool _isChecked;
        public string Display { get; set; }
        public T Value { get; set; }
        public bool IsChecked
        {
            get => _isChecked;
            set => _isChecked = value;
        }
    }
}