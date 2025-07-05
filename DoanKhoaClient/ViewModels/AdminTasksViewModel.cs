using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
using DoanKhoaClient.Helpers; // Thêm dòng này
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace DoanKhoaClient.ViewModels
{
    public class AdminTasksViewModel : INotifyPropertyChanged
    {
        private readonly TaskService _taskService;
        private ObservableCollection<TaskSession> _sessions;
        private TaskSession _selectedSession;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TaskSession> Sessions
        {
            get => _sessions;
            set
            {
                _sessions = value;
                OnPropertyChanged(nameof(Sessions));
            }
        }

        public TaskSession SelectedSession
        {
            get => _selectedSession;
            set
            {
                _selectedSession = value;
                OnPropertyChanged(nameof(SelectedSession));
                // Cập nhật trạng thái các command
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                // Cập nhật trạng thái các command khi IsLoading thay đổi
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Commands
        public ICommand CreateSessionCommand { get; }
        public ICommand EditSessionCommand { get; }
        public ICommand DeleteSessionCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewSessionDetailsCommand { get; }
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToChatCommand { get; }
        public ICommand NavigateToActivitiesCommand { get; }
        public ICommand NavigateToMembersCommand { get; }

        public AdminTasksViewModel(TaskService taskService = null)
        {
            _taskService = taskService ?? new TaskService();
            Sessions = new ObservableCollection<TaskSession>();

            // Khởi tạo các command hiện tại
            CreateSessionCommand = new DoanKhoaClient.Helpers.RelayCommand(_ => ExecuteCreateSessionAsync(), _ => !IsLoading);
            EditSessionCommand = new DoanKhoaClient.Helpers.RelayCommand(ExecuteEditSession, CanExecuteSessionAction);
            DeleteSessionCommand = new DoanKhoaClient.Helpers.RelayCommand(ExecuteDeleteSessionAsync, CanExecuteSessionAction);
            RefreshCommand = new DoanKhoaClient.Helpers.RelayCommand(_ => LoadSessionsAsync(), _ => !IsLoading);
            ViewSessionDetailsCommand = new DoanKhoaClient.Helpers.RelayCommand(ExecuteViewSessionDetails, CanExecuteSessionAction);

            // Khởi tạo các navigation command mới
            NavigateToHomeCommand = new DoanKhoaClient.Helpers.RelayCommand(ExecuteNavigateToHome);
            NavigateToChatCommand = new DoanKhoaClient.Helpers.RelayCommand(ExecuteNavigateToChat);
            NavigateToActivitiesCommand = new DoanKhoaClient.Helpers.RelayCommand(ExecuteNavigateToActivities);
            NavigateToMembersCommand = new DoanKhoaClient.Helpers.RelayCommand(ExecuteNavigateToMembers);

            // Tải dữ liệu
            LoadSessionsAsync();
        }

        // Thực thi các navigation command
        private void ExecuteNavigateToHome(object parameter)
        {
            // Mở trang chủ
            var homeView = new HomePageView();
            homeView.Show();

            // Đóng trang hiện tại
            CloseCurrentWindow();
        }

        private void ExecuteNavigateToChat(object parameter)
        {
            // Mở trang chat
            var chatView = new UserChatView();
            chatView.Show();

            // Đóng trang hiện tại
            CloseCurrentWindow();
        }

        private void ExecuteNavigateToActivities(object parameter)
        {
            // Mở trang hoạt động
            var activitiesView = new AdminActivitiesView();
            activitiesView.Show();

            // Đóng trang hiện tại
            CloseCurrentWindow();
        }

        private void ExecuteNavigateToMembers(object parameter)
        {
            // Mở trang thành viên
            var membersView = new HomePageView();
            membersView.Show();

            // Đóng trang hiện tại
            CloseCurrentWindow();
        }

        // Phương thức đóng cửa sổ hiện tại
        private void CloseCurrentWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is AdminTasksView)
                {
                    window.Close();
                    break;
                }
            }
        }


        private async Task LoadSessionsAsync()
        {
            try
            {
                IsLoading = true;
                var sessions = await _taskService.GetTaskSessionsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Sessions = new ObservableCollection<TaskSession>(sessions.OrderByDescending(s => s.UpdatedAt));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách phiên làm việc: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteSessionAction(object param)
        {
            return !IsLoading && param is TaskSession;
        }

        private async void ExecuteCreateSessionAsync()
        {
            var dialog = new CreateTaskSessionDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;

                    // Chuẩn bị object trước khi gửi
                    dialog.TaskSession.PrepareForSending();

                    var newSession = await _taskService.CreateTaskSessionAsync(dialog.TaskSession);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Sessions.Insert(0, newSession);
                    });
                    MessageBox.Show("Phiên làm việc đã được tạo thành công.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo phiên làm việc: {ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecuteEditSession(object param)
        {
            if (param is TaskSession session)
            {
                var dialog = new EditTaskSessionDialog(session);
                if (dialog.ShowDialog() == true)
                {
                    EditSessionAsync(dialog.TaskSession);
                }
            }
        }

        private async void EditSessionAsync(TaskSession updatedSession)
        {
            try
            {
                IsLoading = true;

                // Đảm bảo cập nhật thời gian
                updatedSession.UpdatedAt = DateTime.Now;

                var result = await _taskService.UpdateTaskSessionAsync(updatedSession.Id, updatedSession);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var index = Sessions.IndexOf(Sessions.FirstOrDefault(s => s.Id == result.Id));
                    if (index >= 0)
                    {
                        Sessions[index] = result;
                    }
                });

                MessageBox.Show("Phiên làm việc đã được cập nhật thành công.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật phiên làm việc: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteDeleteSessionAsync(object param)
        {
            if (param is TaskSession session)
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa phiên làm việc '{session.Name}'?\n\nLưu ý: Tất cả chương trình và công việc liên quan cũng sẽ bị xóa.",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        IsLoading = true;
                        await _taskService.DeleteTaskSessionAsync(session.Id);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Sessions.Remove(session);
                        });

                        MessageBox.Show("Phiên làm việc đã được xóa thành công.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa phiên làm việc: {ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
        }

        private void ExecuteViewSessionDetails(object param)
        {
            if (param is TaskSession session)
            {
                try
                {
                    Window programsView = null;

                    // Debug log để kiểm tra Type
                    System.Diagnostics.Debug.WriteLine($"Opening session details for: {session.Name}, Type: {session.Type}");

                    switch (session.Type)
                    {
                        case TaskSessionType.Event:
                            programsView = new AdminTaskGroupTaskEventView(session);
                            break;
                        case TaskSessionType.Study:
                            programsView = new AdminTaskGroupTaskStudyView(session);
                            break;
                        case TaskSessionType.Design:
                            programsView = new AdminTaskGroupTaskDesignView(session);
                            break;
                        default:
                            MessageBox.Show($"Không hỗ trợ loại phiên làm việc: {session.Type} (Value: {(int)session.Type})",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                    }

                    if (programsView != null)
                    {
                        try
                        {
                            // Đảm bảo tài nguyên được tải trước khi hiển thị cửa sổ
                            programsView.Resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("/DoanKhoaClient;component/Resources/TaskViewResources.xaml", UriKind.Relative)
                            });

                            // Đặt vị trí cửa sổ mới ở giữa màn hình
                            programsView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            programsView.Show();
                        }
                        catch (Exception resourceEx)
                        {
                            MessageBox.Show($"Lỗi tải tài nguyên: {resourceEx.Message}",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi mở trang chi tiết: {ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}