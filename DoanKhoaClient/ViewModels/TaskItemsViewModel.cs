using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
using DoanKhoaClient.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.ViewModels
{
    public class TaskItemsViewModel : INotifyPropertyChanged
    {
        private readonly TaskService _taskService;
        private ObservableCollection<TaskItem> _taskItems;
        private TaskProgram _program;
        private bool _isLoading;
        private TaskItem _selectedTaskItem;
        private bool _isExecutingCommand = false;
        private DateTime _lastCommandExecutionTime = DateTime.MinValue;
        private readonly TimeSpan _commandDelayTime = TimeSpan.FromSeconds(1);

        public event PropertyChangedEventHandler PropertyChanged;

        public TaskProgram Program
        {
            get => _program;
            set
            {
                _program = value;
                OnPropertyChanged(nameof(Program));
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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public TaskItem SelectedTaskItem
        {
            get => _selectedTaskItem;
            set
            {
                _selectedTaskItem = value;
                OnPropertyChanged(nameof(SelectedTaskItem));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand CreateTaskItemCommand { get; }
        public ICommand EditTaskItemCommand { get; }
        public ICommand DeleteTaskItemCommand { get; }
        public ICommand CompleteTaskItemCommand { get; }
        public ICommand RefreshCommand { get; }

        public TaskItemsViewModel(TaskProgram program, TaskService taskService = null)
        {
            Debug.WriteLine($"Initializing TaskItemsViewModel with program: {program?.Id ?? "null"}");
            Program = program;
            _taskService = taskService ?? new TaskService();
            TaskItems = new ObservableCollection<TaskItem>();

            // Khởi tạo commands
            CreateTaskItemCommand = new RelayCommand(_ => ExecuteCreateTaskItemAsync(), _ => !IsLoading && !_isExecutingCommand);
            EditTaskItemCommand = new RelayCommand(param => ExecuteEditTaskItemAsync(param), CanExecuteTaskAction);
            DeleteTaskItemCommand = new RelayCommand(param => ExecuteDeleteTaskItemAsync(param), CanExecuteTaskAction);
            CompleteTaskItemCommand = new RelayCommand(param => ExecuteCompleteTaskItemAsync(param), CanExecuteCompleteAction);
            RefreshCommand = new RelayCommand(async _ => await LoadTaskItemsAsync(), _ => !IsLoading && !_isExecutingCommand);

            // Đăng ký sự kiện với service
            _taskService.TaskItemUpdated += OnTaskItemUpdated;

            // Tải danh sách công việc đúng cách
            Debug.WriteLine("Scheduling initial task items load");
            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await LoadTaskItemsAsync();
            }));
        }

        private bool CanExecuteTaskAction(object param)
        {
            if (_isExecutingCommand) return false;

            if (param is TaskItem item)
            {
                return !IsLoading;
            }
            return SelectedTaskItem != null && !IsLoading;
        }

        private bool CanExecuteCompleteAction(object param)
        {
            if (_isExecutingCommand) return false;

            if (param is TaskItem item)
            {
                return item.Status != TaskItemStatus.Completed && item.Status != TaskItemStatus.Canceled;
            }
            return SelectedTaskItem != null &&
                   SelectedTaskItem.Status != TaskItemStatus.Completed &&
                   SelectedTaskItem.Status != TaskItemStatus.Canceled;
        }

        private async Task LoadTaskItemsAsync()
        {
            if (IsLoading) return;

            Debug.WriteLine($"Loading task items for program: {Program?.Id ?? "null"}");
            if (Program == null || string.IsNullOrEmpty(Program.Id))
            {
                Debug.WriteLine("Cannot load task items: Program is null or has no ID");
                return;
            }

            try
            {
                IsLoading = true;
                var items = await _taskService.GetTaskItemsAsync(Program.Id);
                Debug.WriteLine($"Loaded {items.Count} task items");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TaskItems.Clear();
                    foreach (var item in items)
                    {
                        TaskItems.Add(item);
                    }
                    Debug.WriteLine($"Added {TaskItems.Count} items to UI collection");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading task items: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải danh sách công việc: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteCreateTaskItemAsync()
        {
            // Chống click nhiều lần và debounce
            if (_isExecutingCommand) return;
            if ((DateTime.Now - _lastCommandExecutionTime) < _commandDelayTime)
            {
                Debug.WriteLine("Command execution debounced");
                return;
            }

            _lastCommandExecutionTime = DateTime.Now;

            try
            {
                _isExecutingCommand = true;
                Debug.WriteLine("Executing create task item command");

                var dialog = new CreateTaskItemDialog(Program.Id);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        IsLoading = true;
                        Debug.WriteLine($"Creating task: {dialog.TaskItem.Title}, ProgramId: {dialog.TaskItem.ProgramId}");

                        var newItem = await _taskService.CreateTaskItemAsync(dialog.TaskItem);
                        Debug.WriteLine($"Task created with ID: {newItem.Id}");

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Kiểm tra trùng lặp trước khi thêm vào collection
                            var existingItem = TaskItems.FirstOrDefault(i => i.Id == newItem.Id);
                            if (existingItem == null)
                            {
                                TaskItems.Insert(0, newItem);
                                Debug.WriteLine($"Added new task to UI collection: {newItem.Id}");
                            }
                            else
                            {
                                Debug.WriteLine($"Task already exists in UI collection: {newItem.Id}");
                            }
                        });

                        MessageBox.Show("Công việc đã được tạo thành công.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception in ExecuteCreateTaskItemAsync: {ex}");
                        MessageBox.Show($"Lỗi khi tạo công việc: {ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
                else
                {
                    Debug.WriteLine("Create task dialog canceled");
                }
            }
            finally
            {
                _isExecutingCommand = false;
            }
        }

        private async void ExecuteEditTaskItemAsync(object param)
        {
            // Chống click nhiều lần và debounce
            if (_isExecutingCommand) return;
            if ((DateTime.Now - _lastCommandExecutionTime) < _commandDelayTime)
            {
                Debug.WriteLine("Command execution debounced");
                return;
            }

            _lastCommandExecutionTime = DateTime.Now;

            try
            {
                _isExecutingCommand = true;

                var taskItem = param as TaskItem ?? SelectedTaskItem;
                if (taskItem == null)
                {
                    Debug.WriteLine("No task item selected for edit");
                    MessageBox.Show("Vui lòng chọn một công việc để chỉnh sửa.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Tạo bản sao của task item để tránh thay đổi trực tiếp
                var taskItemCopy = new TaskItem
                {
                    Id = taskItem.Id,
                    Title = taskItem.Title,
                    Description = taskItem.Description,
                    DueDate = taskItem.DueDate,
                    AssignedToId = taskItem.AssignedToId,
                    AssignedToName = taskItem.AssignedToName,
                    ProgramId = taskItem.ProgramId,
                    CreatedAt = taskItem.CreatedAt,
                    UpdatedAt = DateTime.Now,
                    CompletedAt = taskItem.CompletedAt,
                    Status = taskItem.Status,
                    Priority = taskItem.Priority
                };

                Debug.WriteLine($"Editing task: {taskItemCopy.Id}, Title: {taskItemCopy.Title}");
                var dialog = new EditTaskItemDialog(taskItemCopy);

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        IsLoading = true;

                        // Đảm bảo ID được giữ nguyên và log ra để debug
                        string itemId = dialog.TaskItem.Id;
                        if (string.IsNullOrEmpty(itemId))
                        {
                            throw new Exception("Task ID is null or empty");
                        }

                        Debug.WriteLine($"Updating task with ID: {itemId}");
                        Debug.WriteLine($"Task details: Title={dialog.TaskItem.Title}, Status={dialog.TaskItem.Status}");

                        var result = await _taskService.UpdateTaskItemAsync(itemId, dialog.TaskItem);
                        Debug.WriteLine($"Task updated successfully: {result?.Id ?? "null"}");

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (result != null)
                            {
                                var index = -1;
                                var existing = TaskItems.FirstOrDefault(i => i.Id == result.Id);
                                if (existing != null)
                                {
                                    index = TaskItems.IndexOf(existing);
                                }

                                if (index >= 0)
                                {
                                    TaskItems[index] = result;
                                    Debug.WriteLine($"Updated task in UI collection at index: {index}");
                                }
                                else
                                {
                                    Debug.WriteLine($"Task not found in UI collection: {result.Id}");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Server returned null result");
                            }
                        });

                        MessageBox.Show("Công việc đã được cập nhật thành công.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception in ExecuteEditTaskItemAsync: {ex}");
                        MessageBox.Show($"Lỗi khi cập nhật công việc: {ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
                else
                {
                    Debug.WriteLine("Edit task dialog canceled");
                }
            }
            finally
            {
                _isExecutingCommand = false;
            }
        }

        private async void ExecuteDeleteTaskItemAsync(object param)
        {
            // Chống click nhiều lần và debounce
            if (_isExecutingCommand) return;
            if ((DateTime.Now - _lastCommandExecutionTime) < _commandDelayTime)
            {
                Debug.WriteLine("Command execution debounced");
                return;
            }

            _lastCommandExecutionTime = DateTime.Now;

            try
            {
                _isExecutingCommand = true;

                var taskItem = param as TaskItem ?? SelectedTaskItem;
                if (taskItem == null)
                {
                    Debug.WriteLine("No task item selected for deletion");
                    MessageBox.Show("Vui lòng chọn một công việc để xóa.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Debug.WriteLine($"Confirming delete for task: {taskItem.Id}, Title: {taskItem.Title}");
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa công việc '{taskItem.Title}'?",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        IsLoading = true;
                        Debug.WriteLine($"Deleting task: {taskItem.Id}");

                        bool success = await _taskService.DeleteTaskItemAsync(taskItem.Id);
                        Debug.WriteLine($"Task deletion result: {success}");

                        if (success)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                TaskItems.Remove(taskItem);
                                Debug.WriteLine("Removed task from UI collection");

                                // Nếu item bị xóa là item đang được chọn, reset selection
                                if (SelectedTaskItem == taskItem)
                                {
                                    SelectedTaskItem = null;
                                    Debug.WriteLine("Reset selection after deletion");
                                }
                            });

                            MessageBox.Show("Công việc đã được xóa thành công.",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            Debug.WriteLine("Delete operation returned false");
                            MessageBox.Show("Không thể xóa công việc. Vui lòng thử lại sau.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception in ExecuteDeleteTaskItemAsync: {ex}");
                        MessageBox.Show($"Lỗi khi xóa công việc: {ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
                else
                {
                    Debug.WriteLine("Delete operation canceled by user");
                }
            }
            finally
            {
                _isExecutingCommand = false;
            }
        }

        private async void ExecuteCompleteTaskItemAsync(object param)
        {
            // Chống click nhiều lần và debounce
            if (_isExecutingCommand) return;
            if ((DateTime.Now - _lastCommandExecutionTime) < _commandDelayTime)
            {
                Debug.WriteLine("Command execution debounced");
                return;
            }

            _lastCommandExecutionTime = DateTime.Now;

            try
            {
                _isExecutingCommand = true;

                var taskItem = param as TaskItem ?? SelectedTaskItem;
                if (taskItem == null)
                {
                    Debug.WriteLine("No task item selected for completion");
                    return;
                }

                try
                {
                    IsLoading = true;
                    Debug.WriteLine($"Marking task as complete: {taskItem.Id}, Title: {taskItem.Title}");

                    var result = await _taskService.CompleteTaskItemAsync(taskItem.Id);
                    Debug.WriteLine($"Task completion result: {result.Id}, Status: {result.Status}");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var index = TaskItems.IndexOf(TaskItems.FirstOrDefault(i => i.Id == result.Id));
                        if (index >= 0)
                        {
                            TaskItems[index] = result;
                            Debug.WriteLine($"Updated task status in UI collection at index: {index}");
                        }
                        else
                        {
                            Debug.WriteLine($"Task not found in UI collection: {result.Id}");
                        }
                    });

                    MessageBox.Show("Công việc đã được đánh dấu hoàn thành.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in ExecuteCompleteTaskItemAsync: {ex}");
                    MessageBox.Show($"Lỗi khi hoàn thành công việc: {ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
            finally
            {
                _isExecutingCommand = false;
            }
        }

        private void OnTaskItemUpdated(TaskItem item)
        {
            if (item == null)
            {
                Debug.WriteLine("Received null item in OnTaskItemUpdated");
                return;
            }

            Debug.WriteLine($"Task updated event received: {item.Id}, ProgramId: {item.ProgramId}");

            if (Program == null || item.ProgramId != Program.Id)
            {
                Debug.WriteLine("Task update ignored - different program");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingItem = TaskItems.FirstOrDefault(i => i.Id == item.Id);
                if (existingItem != null)
                {
                    var index = TaskItems.IndexOf(existingItem);
                    TaskItems[index] = item;
                    Debug.WriteLine($"Updated existing task in UI collection at index: {index}");
                }
                else
                {
                    TaskItems.Add(item);
                    Debug.WriteLine($"Added new task to UI collection from update event: {item.Id}");
                }
            });
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}