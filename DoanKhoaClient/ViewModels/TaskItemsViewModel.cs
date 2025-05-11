using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            }
        }

        // Commands
        public ICommand CreateTaskItemCommand { get; }
        public ICommand EditTaskItemCommand { get; }
        public ICommand DeleteTaskItemCommand { get; }
        public ICommand CompleteTaskItemCommand { get; }
        public ICommand RefreshCommand { get; }

        public TaskItemsViewModel(TaskProgram program, TaskService taskService = null)
        {
            _taskService = taskService ?? new TaskService();
            Program = program;
            TaskItems = new ObservableCollection<TaskItem>();

            // Khởi tạo các command
            CreateTaskItemCommand = new RelayCommand(_ => ExecuteCreateTaskItemAsync());
            EditTaskItemCommand = new RelayCommand(ExecuteEditTaskItem, CanExecuteAction);
            DeleteTaskItemCommand = new RelayCommand(ExecuteDeleteTaskItemAsync, CanExecuteAction);
            CompleteTaskItemCommand = new RelayCommand(ExecuteCompleteTaskItemAsync, CanExecuteCompleteAction);
            RefreshCommand = new RelayCommand(_ => LoadTaskItemsAsync());

            // Tải dữ liệu
            LoadTaskItemsAsync();
        }

        private async Task LoadTaskItemsAsync()
        {
            try
            {
                IsLoading = true;
                var items = await _taskService.GetTaskItemsAsync(Program.Id);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TaskItems = new ObservableCollection<TaskItem>(items.OrderByDescending(i => i.Status == TaskItemStatus.Pending)
        .ThenBy(i => i.DueDate));
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

        private bool CanExecuteAction(object param)
        {
            return param != null;
        }

        private bool CanExecuteCompleteAction(object param)
        {
            if (param is TaskItem item)
            {
                return item.Status == TaskItemStatus.Pending || item.Status == TaskItemStatus.InProgress;
            }
            return false;
        }

        private async Task ExecuteCreateTaskItemAsync()
        {
            var dialog = new CreateTaskItemDialog(Program.Id);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    var newItem = await _taskService.CreateTaskItemAsync(dialog.TaskItem);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TaskItems.Insert(0, newItem);
                    });
                    MessageBox.Show("Công việc đã được tạo thành công.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo công việc: {ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecuteEditTaskItem(object param)
        {
            if (param is TaskItem item)
            {
                var dialog = new EditTaskItemDialog(item);
                if (dialog.ShowDialog() == true)
                {
                    EditTaskItemAsync(dialog.TaskItem);
                }
            }
        }

        private async void EditTaskItemAsync(TaskItem updatedItem)
        {
            try
            {
                IsLoading = true;
                var result = await _taskService.UpdateTaskItemAsync(updatedItem.Id, updatedItem);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var index = TaskItems.IndexOf(TaskItems.FirstOrDefault(i => i.Id == result.Id));
                    if (index >= 0)
                    {
                        TaskItems[index] = result;
                    }
                });

                MessageBox.Show("Công việc đã được cập nhật thành công.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật công việc: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteDeleteTaskItemAsync(object param)
        {
            if (param is TaskItem item)
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa công việc '{item.Title}'?",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        IsLoading = true;
                        await _taskService.DeleteTaskItemAsync(item.Id);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TaskItems.Remove(item);
                        });

                        MessageBox.Show("Công việc đã được xóa thành công.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa công việc: {ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
        }

        private async void ExecuteCompleteTaskItemAsync(object param)
        {
            if (param is TaskItem item)
            {
                var result = MessageBox.Show(
                    $"Đánh dấu công việc '{item.Title}' là đã hoàn thành?",
                    "Xác nhận hoàn thành",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        IsLoading = true;
                        var completedItem = await _taskService.CompleteTaskItemAsync(item.Id);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var index = TaskItems.IndexOf(TaskItems.FirstOrDefault(i => i.Id == completedItem.Id));
                            if (index >= 0)
                            {
                                TaskItems[index] = completedItem;
                            }
                        });

                        MessageBox.Show("Công việc đã được đánh dấu hoàn thành.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi hoàn thành công việc: {ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}