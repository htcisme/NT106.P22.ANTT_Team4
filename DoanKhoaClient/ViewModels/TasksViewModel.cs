using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics; 
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DoanKhoaClient.ViewModels
{
    public class TasksViewModel : INotifyPropertyChanged
    {
        private readonly TaskService _taskService;
        private ObservableCollection<TaskSession> _sessions;
        private TaskSession _selectedSession;
        private ObservableCollection<TaskProgram> _programs;
        private TaskProgram _selectedProgram;
        private ObservableCollection<TaskItem> _taskItems;
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
                if (_selectedSession != null)
                {
                    LoadProgramsAsync(_selectedSession.Id);
                }
                else
                {
                    Programs = new ObservableCollection<TaskProgram>();
                }
            }
        }

        public ObservableCollection<TaskProgram> Programs
        {
            get => _programs;
            set
            {
                _programs = value;
                OnPropertyChanged(nameof(Programs));
            }
        }

        public TaskProgram SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                _selectedProgram = value;
                OnPropertyChanged(nameof(SelectedProgram));
                if (_selectedProgram != null)
                {
                    LoadTaskItemsAsync(_selectedProgram.Id);
                }
                else
                {
                    TaskItems = new ObservableCollection<TaskItem>();
                }
            }
        }

        public ObservableCollection<TaskItem> TaskItems
        {
            get => _taskItems;
            set
            {
                _taskItems = value;
                OnPropertyChanged(nameof(TaskItems));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public TasksViewModel(TaskService taskService = null)
        {
            _taskService = taskService ?? new TaskService();
            Sessions = new ObservableCollection<TaskSession>();
            Programs = new ObservableCollection<TaskProgram>();
            TaskItems = new ObservableCollection<TaskItem>();

            // Đăng ký sự kiện cập nhật từ TaskService
            _taskService.TaskSessionUpdated += OnTaskSessionUpdated;
            _taskService.TaskProgramUpdated += OnTaskProgramUpdated;
            _taskService.TaskItemUpdated += OnTaskItemUpdated;

            // Tải dữ liệu
            LoadSessionsAsync();
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

        private async Task LoadProgramsAsync(string sessionId)
        {
            try
            {
                IsLoading = true;
                var programs = await _taskService.GetTaskProgramsAsync(sessionId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Programs = new ObservableCollection<TaskProgram>(programs.OrderBy(p => p.StartDate));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách chương trình: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadTaskItemsAsync(string programId)
        {
            try
            {
                IsLoading = true;

                // Log để debug
                Debug.WriteLine($"Loading tasks for program: {programId}");

                var items = await _taskService.GetTaskItemsAsync(programId);

                // Log kết quả để debug
                Debug.WriteLine($"Loaded {items.Count} tasks");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Đơn giản hóa cách sắp xếp có thể giúp tránh lỗi
                    TaskItems = new ObservableCollection<TaskItem>(items);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách công việc: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnTaskSessionUpdated(TaskSession session)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
                if (existingSession != null)
                {
                    var index = Sessions.IndexOf(existingSession);
                    Sessions[index] = session;
                }
                else
                {
                    Sessions.Insert(0, session);
                }
            });
        }

        private void OnTaskProgramUpdated(TaskProgram program)
        {
            if (SelectedSession?.Id == program.SessionId)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingProgram = Programs.FirstOrDefault(p => p.Id == program.Id);
                    if (existingProgram != null)
                    {
                        var index = Programs.IndexOf(existingProgram);
                        Programs[index] = program;
                    }
                    else
                    {
                        Programs.Add(program);
                    }
                });
            }
        }

        private void OnTaskItemUpdated(TaskItem item)
        {
            if (SelectedProgram?.Id == item.ProgramId)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingItem = TaskItems.FirstOrDefault(i => i.Id == item.Id);
                    if (existingItem != null)
                    {
                        var index = TaskItems.IndexOf(existingItem);
                        TaskItems[index] = item;
                    }
                    else
                    {
                        TaskItems.Add(item);
                    }
                });
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}