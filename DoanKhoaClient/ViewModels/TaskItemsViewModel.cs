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
        private ObservableCollection<TaskItem> _selectedTaskItems; // ✅ THÊM: Multi-select support

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
        public ObservableCollection<TaskItem> SelectedTaskItems
        {
            get => _selectedTaskItems;
            set
            {
                _selectedTaskItems = value;
                OnPropertyChanged(nameof(SelectedTaskItems));
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(SelectedItemsCount));
            }
        }

        public bool HasSelectedItems => SelectedTaskItems?.Count > 0;
        public int SelectedItemsCount => SelectedTaskItems?.Count ?? 0;

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
        public ICommand SendReminderCommand { get; }
        public ICommand SendBulkReminderCommand { get; } // ✅ THÊM: Bulk reminder command
        public ICommand TestEmailCommand { get; }
        public ICommand SelectAllCommand { get; } // ✅ THÊM: Select all command
        public ICommand ClearSelectionCommand { get; } // ✅ THÊM: Clear selection command


        public TaskItemsViewModel(TaskProgram program, TaskService taskService = null)
        {
            Debug.WriteLine($"Initializing TaskItemsViewModel with program: {program?.Id ?? "null"}");
            Program = program;
            _taskService = taskService ?? new TaskService();
            TaskItems = new ObservableCollection<TaskItem>();
            _selectedTaskItems = new ObservableCollection<TaskItem>(); // ✅ THÊM: Initialize


            // Khởi tạo commands
            CreateTaskItemCommand = new RelayCommand(_ => ExecuteCreateTaskItemAsync(), _ => !IsLoading && !_isExecutingCommand);
            EditTaskItemCommand = new RelayCommand(param => ExecuteEditTaskItemAsync(param), CanExecuteTaskAction);
            DeleteTaskItemCommand = new RelayCommand(param => ExecuteDeleteTaskItemAsync(param), CanExecuteTaskAction);
            CompleteTaskItemCommand = new RelayCommand(param => ExecuteCompleteTaskItemAsync(param), CanExecuteCompleteAction);
            RefreshCommand = new RelayCommand(async _ => await LoadTaskItemsAsync(), _ => !IsLoading && !_isExecutingCommand);
            SelectAllCommand = new RelayCommand(_ => ExecuteSelectAll()); // ✅ FIX: Add parameter
            ClearSelectionCommand = new RelayCommand(_ => ExecuteClearSelection()); // ✅ FIX: Add parameter
                                                                                    // Đăng ký sự kiện với service

            SendReminderCommand = new RelayCommand(
            async param =>
            {
                Debug.WriteLine($"SendReminderCommand executed with param: {param?.GetType().Name}");
                await ExecuteSendReminderAsync(param as TaskItem);
            },
            param =>
            {
                var canExecute = CanExecuteTaskItemAction(param as TaskItem);
                Debug.WriteLine($"SendReminderCommand CanExecute: {canExecute} for {(param as TaskItem)?.Title}");
                return canExecute;
            }
        );

            // ✅ THÊM: SendBulkReminderCommand - ĐOẠN NÀY ĐANG THIẾU!
            SendBulkReminderCommand = new RelayCommand(
                async _ =>
                {
                    Debug.WriteLine($"SendBulkReminderCommand executed with {SelectedItemsCount} items");
                    await ExecuteSendBulkReminderAsync();
                },
                _ =>
                {
                    var canExecute = HasSelectedItems && !IsLoading;
                    Debug.WriteLine($"SendBulkReminderCommand CanExecute: {canExecute} (HasSelectedItems: {HasSelectedItems}, IsLoading: {IsLoading})");
                    return canExecute;
                }
            );
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
                    SelectedTaskItems.Clear();
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
        private async Task ExecuteSendReminderAsync(TaskItem item)
        {
            try
            {
                Debug.WriteLine($"===== Sending Reminder for TaskItem =====");
                Debug.WriteLine($"TaskItem ID: {item?.Id}");
                Debug.WriteLine($"Title: {item?.Title}");
                Debug.WriteLine($"AssignedToEmail: {item?.AssignedToEmail}");

                if (item == null)
                {
                    MessageBox.Show("Vui lòng chọn một công việc để gửi nhắc nhở", "Cảnh báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(item.AssignedToEmail))
                {
                    MessageBox.Show($"Công việc '{item.Title}' chưa có email người thực hiện.\nVui lòng cập nhật email trước khi gửi nhắc nhở.",
                        "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Gửi email nhắc nhở cho:\n\n" +
                    $"📝 Công việc: {item.Title}\n" +
                    $"👤 Người thực hiện: {item.AssignedToName}\n" +
                    $"📧 Email: {item.AssignedToEmail}\n" +
                    $"📅 Hạn chót: {item.DueDate:dd/MM/yyyy}\n\n" +
                    $"Bạn có muốn gửi nhắc nhở không?",
                    "Xác nhận gửi nhắc nhở",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    var success = await _taskService.SendTaskReminderAsync(item.Id);

                    if (success)
                    {
                        Debug.WriteLine("✅ Reminder sent successfully");
                        MessageBox.Show($"✅ Email nhắc nhở đã được gửi thành công!\n\nGửi tới: {item.AssignedToEmail}",
                            "Gửi thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadTaskItemsAsync();
                    }
                    else
                    {
                        Debug.WriteLine("❌ Failed to send reminder");
                        MessageBox.Show($"❌ Không thể gửi email nhắc nhở.\n\nVui lòng kiểm tra cấu hình email.",
                            "Gửi thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in ExecuteSendReminderAsync: {ex.Message}");
                MessageBox.Show($"❌ Lỗi khi gửi nhắc nhở: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ✅ THÊM: Bulk reminder implementation
        private async Task ExecuteSendBulkReminderAsync()
        {
            try
            {
                Debug.WriteLine($"===== Sending Bulk Reminders =====");
                Debug.WriteLine($"Selected items count: {SelectedTaskItems?.Count ?? 0}");

                if (SelectedTaskItems == null || !SelectedTaskItems.Any())
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một công việc để gửi nhắc nhở", "Cảnh báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for items without email
                var itemsWithoutEmail = SelectedTaskItems.Where(x => string.IsNullOrWhiteSpace(x.AssignedToEmail)).ToList();
                if (itemsWithoutEmail.Any())
                {
                    var itemNames = string.Join("\n", itemsWithoutEmail.Select(x => $"- {x.Title}"));
                    MessageBox.Show($"Các công việc sau chưa có email người thực hiện:\n\n{itemNames}\n\nVui lòng cập nhật email trước khi gửi nhắc nhở.",
                        "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show confirmation
                var selectedTitles = string.Join("\n", SelectedTaskItems.Select(x => $"- {x.Title} → {x.AssignedToEmail}"));
                var result = MessageBox.Show(
                    $"Gửi email nhắc nhở cho {SelectedTaskItems.Count} công việc:\n\n{selectedTitles}\n\nBạn có muốn tiếp tục không?",
                    "Xác nhận gửi nhắc nhở hàng loạt",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;

                    var taskIds = SelectedTaskItems.Select(x => x.Id).ToList();
                    var bulkResult = await _taskService.SendBulkTaskRemindersAsync(taskIds);

                    Debug.WriteLine($"✅ Bulk reminder completed:");
                    Debug.WriteLine($"  - Total: {bulkResult.TotalProcessed}");
                    Debug.WriteLine($"  - Success: {bulkResult.SuccessCount}");
                    Debug.WriteLine($"  - Failed: {bulkResult.FailCount}");

                    // Show detailed results
                    var resultMessage = $"📊 Kết quả gửi nhắc nhở:\n\n" +
                                      $"✅ Thành công: {bulkResult.SuccessCount}\n" +
                                      $"❌ Thất bại: {bulkResult.FailCount}\n" +
                                      $"📝 Tổng cộng: {bulkResult.TotalProcessed}";

                    if (bulkResult.FailCount > 0)
                    {
                        var failedItems = bulkResult.Results
                            .Where(x => !x.Success)
                            .Select(x => $"- {x.TaskTitle}: {x.Message}")
                            .Take(5); // Show first 5 failures

                        resultMessage += $"\n\n❌ Các lỗi:\n{string.Join("\n", failedItems)}";

                        if (bulkResult.FailCount > 5)
                        {
                            resultMessage += $"\n... và {bulkResult.FailCount - 5} lỗi khác";
                        }
                    }

                    MessageBox.Show(resultMessage, "Kết quả gửi nhắc nhở",
                        MessageBoxButton.OK,
                        bulkResult.FailCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                    // Clear selection and refresh
                    SelectedTaskItems.Clear();
                    await LoadTaskItemsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in ExecuteSendBulkReminderAsync: {ex.Message}");
                MessageBox.Show($"❌ Lỗi khi gửi nhắc nhở hàng loạt: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ✅ THÊM: Select all tasks
        private void ExecuteSelectAll()
        {
            try
            {
                SelectedTaskItems.Clear();
                foreach (var item in TaskItems)
                {
                    SelectedTaskItems.Add(item);
                }
                Debug.WriteLine($"Selected all {SelectedTaskItems.Count} tasks");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting all: {ex.Message}");
            }
        }

        // ✅ THÊM: Clear selection
        private void ExecuteClearSelection()
        {
            try
            {
                SelectedTaskItems.Clear();
                Debug.WriteLine("Cleared all selections");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing selection: {ex.Message}");
            }
        }
        private bool CanExecuteTaskItemAction(TaskItem item)
        {
            return item != null && !IsLoading;
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
                    MessageBox.Show($"Marking task as complete: {taskItem.Id}, Title: {taskItem.Title}");

                    var result = await _taskService.CompleteTaskItemAsync(taskItem.Id);
                    MessageBox.Show($"Task completion result: {result.Id}, Status: {result.Status}");

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