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

        public ICommand CreateActivityCommand { get; }
        public ICommand EditActivityCommand { get; }
        public ICommand DeleteActivityCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenActivityDetailCommand { get; }

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

            CreateActivityCommand = new RelayCommand(async _ => await ExecuteCreateActivityAsync(), _ => !IsLoading);
            EditActivityCommand = new RelayCommand(async param => await ExecuteEditActivityAsync(param as Activity), param => !IsLoading && param is Activity);
            DeleteActivityCommand = new RelayCommand(async param => await ExecuteDeleteActivityAsync(param as Activity), param => !IsLoading && param is Activity);
            RefreshCommand = new RelayCommand(async _ => await LoadActivitiesAsync(), _ => !IsLoading);
            OpenActivityDetailCommand = new RelayCommand(param => ExecuteOpenActivityDetail(param as Activity), param => param is Activity);
            _ = LoadActivitiesAsync();
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
                    Opacity = 1.0
                },
                new FeaturedActivity
                {
                    Title = "NGỌN ĐUỐC XANH 2025",
                    ImgUrl = "/Views/Images/ndx.png",
                    Width = 175,
                    Height = 175,
                    Opacity = 1.0
                },
                new FeaturedActivity
                {
                    Title = "NETSEC DAY 2024",
                    ImgUrl = "/Views/Images/netsec.png",
                    Width = 175,
                    Height = 175,
                    Opacity = 1.0
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

            for (double o = 1.0; o >= 0; o -= 0.05)
            {
                foreach (var fa in FeaturedActivities)
                    fa.Opacity = o;
                await Task.Delay(9); // tổng ~180ms
            }
            foreach (var fa in FeaturedActivities)
                fa.Opacity = 0;

            // Đổi vị trí: xoay carousel
            var first = FeaturedActivities[0];
            FeaturedActivities.RemoveAt(0);
            FeaturedActivities.Add(first);

            // Xoay PaginationDots cùng chiều với ảnh
            var firstDot = PaginationDots[0];
            PaginationDots.RemoveAt(0);
            PaginationDots.Add(firstDot);


            for (double o = 0; o <= 1.0; o += 0.05)
            {
                foreach (var fa in FeaturedActivities)
                    fa.Opacity = o;
                await Task.Delay(9);
            }
            foreach (var fa in FeaturedActivities)
                fa.Opacity = 1.0;


            // Cập nhật dot
            for (int i = 0; i < PaginationDots.Count; i++)
            {
                PaginationDots[i].IsActive = (i == 0); // dot đầu tiên active
                PaginationDots[i].Color = (i == 0)
                    ? new SolidColorBrush(Color.FromRgb(89, 124, 162))
                    : new SolidColorBrush(Color.FromRgb(219, 236, 247));
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

        private void FilterActivities()
        {
            if (Activities == null) return;
            var filtered = Activities.Where(a =>
                (string.IsNullOrWhiteSpace(SearchText) ||
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
                ) &&
                (!SelectedTypeItem?.Value.HasValue ?? true || a.Type == SelectedTypeItem.Value) &&
                (!SelectedStatusItem?.Value.HasValue ?? true || a.Status == SelectedStatusItem.Value) &&
                (!DateFrom.HasValue || a.Date >= DateFrom.Value) &&
                (!DateTo.HasValue || a.Date <= DateTo.Value)
            ).ToList();
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
} 