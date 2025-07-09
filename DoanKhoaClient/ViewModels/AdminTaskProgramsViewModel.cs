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
using System.Diagnostics;

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
                Debug.WriteLine($"===== AdminTaskProgramsViewModel.LoadProgramsAsync =====");
                Debug.WriteLine($"Session ID: {Session?.Id}");
                Debug.WriteLine($"Filter ProgramType: {_programType} (value: {(int)_programType})");

                IsLoading = true;

                var allPrograms = await _taskService.GetTaskProgramsAsync(Session?.Id ?? "");
                Debug.WriteLine($"Total programs from server: {allPrograms?.Count ?? 0}");

                if (allPrograms != null)
                {
                    foreach (var program in allPrograms)
                    {
                        Debug.WriteLine($"  - Program: '{program.Name}', Type: {program.Type} (value: {(int)program.Type}), ID: {program.Id}");
                    }
                }

                var filteredPrograms = allPrograms?.Where(p => p.Type == _programType).OrderBy(p => p.StartDate).ToList() ?? new List<TaskProgram>();
                Debug.WriteLine($"Filtered programs for {_programType}: {filteredPrograms.Count}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Programs.Clear();
                    foreach (var program in filteredPrograms)
                    {
                        Programs.Add(program);
                        Debug.WriteLine($"  ✅ Added to UI: '{program.Name}' (Type: {program.Type})");
                    }
                });

                Debug.WriteLine($"✅ LoadProgramsAsync completed. Programs in UI: {Programs.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in LoadProgramsAsync: {ex.Message}");
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
            try
            {
                Debug.WriteLine($"===== AdminTaskProgramsViewModel: Creating Program =====");
                Debug.WriteLine($"Target ProgramType: {_programType} (value: {(int)_programType})");
                Debug.WriteLine($"Session: {Session?.Name} (ID: {Session?.Id})");

                // SỬA: Tạo dialog chỉ để nhận input, KHÔNG gọi API
                var dialog = new CreateTaskProgramDialog(_session, _programType, false); // Pass false để không auto-create

                if (dialog.ShowDialog() == true)
                {
                    var programData = dialog.ProgramToCreate;

                    Debug.WriteLine($"Dialog returned program data:");
                    Debug.WriteLine($"  - Name: {programData.Name}");
                    Debug.WriteLine($"  - Type: {programData.Type} (value: {(int)programData.Type})");
                    Debug.WriteLine($"  - SessionId: {programData.SessionId}");

                    IsLoading = true;

                    // SỬA: ViewModel hoàn toàn kiểm soát Type và gọi API
                    programData.Type = _programType; // FORCE set Type đúng
                    programData.SessionId = Session?.Id; // Ensure SessionId đúng

                    Debug.WriteLine($"Final program data before API call:");
                    Debug.WriteLine($"  - Name: {programData.Name}");
                    Debug.WriteLine($"  - Type: {programData.Type} (value: {(int)programData.Type})");
                    Debug.WriteLine($"  - SessionId: {programData.SessionId}");
                    Debug.WriteLine($"  - Expected _programType: {_programType} (value: {(int)_programType})");

                    // SỬA: ViewModel gọi API, không phải dialog
                    var createdProgram = await _taskService.CreateTaskProgramAsync(programData);

                    if (createdProgram != null)
                    {
                        Debug.WriteLine($"✅ API returned created program:");
                        Debug.WriteLine($"  - ID: {createdProgram.Id}");
                        Debug.WriteLine($"  - Name: {createdProgram.Name}");
                        Debug.WriteLine($"  - Type: {createdProgram.Type} (value: {(int)createdProgram.Type})");

                        // Verify Type is correct
                        if (createdProgram.Type != _programType)
                        {
                            Debug.WriteLine($"❌ TYPE MISMATCH! Expected: {_programType} ({(int)_programType}), Got: {createdProgram.Type} ({(int)createdProgram.Type})");
                            MessageBox.Show($"⚠️ Cảnh báo: Program được tạo với Type sai!\n\nMong đợi: {_programType} ({(int)_programType})\nThực tế: {createdProgram.Type} ({(int)createdProgram.Type})",
                                "Type Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            Debug.WriteLine($"✅ Type verification passed!");
                        }

                        // Reload để cập nhật UI
                        await LoadProgramsAsync();

                        MessageBox.Show($"Chương trình '{createdProgram.Name}' đã được tạo thành công!\n\nLoại: {GetProgramTypeText(_programType)} ({(int)_programType})\nID: {createdProgram.Id}",
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Debug.WriteLine("❌ API returned null");
                        MessageBox.Show("Không thể tạo chương trình. API trả về null.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in ExecuteCreateProgramAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Lỗi khi tạo chương trình: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private string GetProgramTypeText(ProgramType type)
        {
            return type switch
            {
                ProgramType.Study => "Học tập",
                ProgramType.Design => "Thiết kế",
                ProgramType.Event => "Sự kiện",
                _ => "Không xác định"
            };
        }
        private void ExecuteEditProgram(object param)
        {
            if (param is TaskProgram program)
            {
                var selectedProgram = SelectedProgram;
                var dialog = new EditTaskProgramDialog(_session, selectedProgram);
                if (dialog.ShowDialog() == true)
                {
                    var index = Programs.IndexOf(selectedProgram);
                    if (index >= 0)
                    {
                        Programs[index] = dialog.Program;
                    }
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