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
    public class AdminTaskProgramsViewModel : INotifyPropertyChanged
    {
        private readonly TaskService _taskService;
        private TaskSession _session;
        private ObservableCollection<TaskProgram> _programs;
        private TaskProgram _selectedProgram;
        private ProgramType _programType;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public TaskSession Session
        {
            get => _session;
            set
            {
                _session = value;
                OnPropertyChanged(nameof(Session));
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
        public ICommand CreateProgramCommand { get; }
        public ICommand EditProgramCommand { get; }
        public ICommand DeleteProgramCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewProgramDetailsCommand { get; }

        public AdminTaskProgramsViewModel(TaskSession session, ProgramType programType, TaskService taskService = null)
        {
            _taskService = taskService ?? new TaskService();
            Session = session;
            _programType = programType;
            Programs = new ObservableCollection<TaskProgram>();

            // Khởi tạo các command
            CreateProgramCommand = new RelayCommand(_ => ExecuteCreateProgramAsync());
            EditProgramCommand = new RelayCommand(ExecuteEditProgram, CanExecuteAction);
            DeleteProgramCommand = new RelayCommand(ExecuteDeleteProgramAsync, CanExecuteAction);
            RefreshCommand = new RelayCommand(_ => LoadProgramsAsync());
            ViewProgramDetailsCommand = new RelayCommand(ExecuteViewProgramDetails, CanExecuteAction);

            // Tải dữ liệu
            LoadProgramsAsync();
        }

        private async Task LoadProgramsAsync()
        {
            try
            {
                IsLoading = true;
                var allPrograms = await _taskService.GetTaskProgramsAsync(Session.Id);
                var filteredPrograms = allPrograms.Where(p => p.Type == _programType).OrderBy(p => p.StartDate);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Programs = new ObservableCollection<TaskProgram>(filteredPrograms);
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

        private bool CanExecuteAction(object param)
        {
            return param != null;
        }

        private async Task ExecuteCreateProgramAsync()
        {
            var dialog = new CreateTaskProgramDialog(Session.Id, _programType);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    var newProgram = await _taskService.CreateTaskProgramAsync(dialog.TaskProgram);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Programs.Add(newProgram);
                    });
                    MessageBox.Show("Chương trình đã được tạo thành công.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo chương trình: {ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecuteEditProgram(object param)
        {
            if (param is TaskProgram program)
            {
                var dialog = new EditTaskProgramDialog(program);
                if (dialog.ShowDialog() == true)
                {
                    EditProgramAsync(dialog.TaskProgram);
                }
            }
        }

        private async void EditProgramAsync(TaskProgram updatedProgram)
        {
            try
            {
                IsLoading = true;
                var result = await _taskService.UpdateTaskProgramAsync(updatedProgram.Id, updatedProgram);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var index = Programs.IndexOf(Programs.FirstOrDefault(p => p.Id == result.Id));
                    if (index >= 0)
                    {
                        Programs[index] = result;
                    }
                });

                MessageBox.Show("Chương trình đã được cập nhật thành công.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật chương trình: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteDeleteProgramAsync(object param)
        {
            if (param is TaskProgram program)
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa chương trình '{program.Name}'?\n\nLưu ý: Tất cả công việc liên quan cũng sẽ bị xóa.",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        IsLoading = true;
                        await _taskService.DeleteTaskProgramAsync(program.Id);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Programs.Remove(program);
                        });

                        MessageBox.Show("Chương trình đã được xóa thành công.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa chương trình: {ex.Message}",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
        }

        private void ExecuteViewProgramDetails(object param)
        {
            if (param is TaskProgram program)
            {
                // Chuyển sang view chi tiết dựa vào loại program
                Window taskItemsView = null;

                switch (program.Type)
                {
                    case ProgramType.Event:
                        taskItemsView = new AdminTasksGroupTaskContentEventView(program);
                        break;
                    case ProgramType.Study:
                        taskItemsView = new AdminTasksGroupTaskContentStudyView(program);
                        break;
                    case ProgramType.Design:
                        taskItemsView = new AdminTasksGroupTaskContentDesignView(program);
                        break;
                }

                if (taskItemsView != null)
                {
                    taskItemsView.Show();
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}