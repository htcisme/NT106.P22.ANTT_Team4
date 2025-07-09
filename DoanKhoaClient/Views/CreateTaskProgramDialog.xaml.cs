using System;
using System.Collections.Generic;
using System.Windows;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using System.Diagnostics;

namespace DoanKhoaClient.Views
{
    public partial class CreateTaskProgramDialog : Window
    {
        private readonly TaskSession _session;
        private readonly TaskService _taskService;
        private readonly ProgramType _programType;
        private readonly bool _autoCreate; // THÊM: Control auto-create behavior

        public TaskProgram ProgramToCreate { get; set; }

        // SỬA: Constructor với auto-create parameter
        public CreateTaskProgramDialog(TaskSession session, ProgramType programType, bool autoCreate = true)
        {
            InitializeComponent();
            _session = session;
            _taskService = new TaskService();
            _programType = programType;
            _autoCreate = autoCreate; // THÊM: Store auto-create mode

            Debug.WriteLine($"===== CreateTaskProgramDialog Constructor =====");
            Debug.WriteLine($"Session ID: {session?.Id}");
            Debug.WriteLine($"ProgramType: {programType} (value: {(int)programType})");
            Debug.WriteLine($"Auto-create mode: {autoCreate}");

            ProgramToCreate = new TaskProgram
            {
                SessionId = session?.Id,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                Type = programType, // Set Type theo parameter
                Status = ProgramStatus.NotStarted,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            DataContext = ProgramToCreate;

            Debug.WriteLine($"ProgramToCreate initialized with Type: {ProgramToCreate.Type} (value: {(int)ProgramToCreate.Type})");

            // Khởi tạo DatePicker
            if (StartDatePicker?.SelectedDate == null)
                StartDatePicker.SelectedDate = DateTime.Today;

            if (EndDatePicker?.SelectedDate == null)
                EndDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        }

        // Backward compatibility constructors
        public CreateTaskProgramDialog(TaskSession session) : this(session, ProgramType.Event, true) { }
        public CreateTaskProgramDialog(TaskSession session, ProgramType programType) : this(session, programType, true) { }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"===== CreateTaskProgramDialog.CreateButton_Click =====");
                Debug.WriteLine($"Auto-create mode: {_autoCreate}");
                Debug.WriteLine($"Current ProgramToCreate.Type: {ProgramToCreate.Type} (value: {(int)ProgramToCreate.Type})");

                // Validation
                if (string.IsNullOrWhiteSpace(ProgramNameTextBox.Text))
                {
                    ShowError("Vui lòng nhập tên chương trình");
                    return;
                }

                if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
                {
                    ShowError("Vui lòng chọn ngày bắt đầu và kết thúc");
                    return;
                }

                if (EndDatePicker.SelectedDate < StartDatePicker.SelectedDate)
                {
                    ShowError("Ngày kết thúc phải sau ngày bắt đầu");
                    return;
                }

                // Cập nhật ProgramToCreate với data từ form
                ProgramToCreate.Name = ProgramNameTextBox.Text.Trim();
                ProgramToCreate.Description = DescriptionTextBox.Text.Trim();
                ProgramToCreate.StartDate = StartDatePicker.SelectedDate.Value;
                ProgramToCreate.EndDate = EndDatePicker.SelectedDate.Value;
                ProgramToCreate.SessionId = _session?.Id;

                // QUAN TRỌNG: Đảm bảo Type đúng
                ProgramToCreate.Type = _programType;

                // Thông tin người thực hiện
                ProgramToCreate.ExecutorId = _session?.Id ?? "system";
                ProgramToCreate.ExecutorName = _session?.ManagerName ?? "Auto Assigned";
                ProgramToCreate.UpdatedAt = DateTime.Now;

                Debug.WriteLine($"Program data prepared:");
                Debug.WriteLine($"  - Name: {ProgramToCreate.Name}");
                Debug.WriteLine($"  - Type: {ProgramToCreate.Type} (value: {(int)ProgramToCreate.Type})");
                Debug.WriteLine($"  - SessionId: {ProgramToCreate.SessionId}");
                Debug.WriteLine($"  - Expected _programType: {_programType} (value: {(int)_programType})");

                if (_autoCreate)
                {
                    Debug.WriteLine("🔄 Auto-create mode: Calling API from dialog");

                    // Auto-create mode: Dialog gọi API
                    var createdProgram = await _taskService.CreateTaskProgramAsync(ProgramToCreate);

                    if (createdProgram != null)
                    {
                        Debug.WriteLine($"✅ Dialog API call successful:");
                        Debug.WriteLine($"  - ID: {createdProgram.Id}");
                        Debug.WriteLine($"  - Type: {createdProgram.Type} (value: {(int)createdProgram.Type})");

                        ProgramToCreate = createdProgram;
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        Debug.WriteLine("❌ Dialog API call failed");
                        ShowError("Không thể tạo chương trình. Server trả về null.");
                    }
                }
                else
                {
                    Debug.WriteLine("📝 Data-only mode: Returning program data to caller");

                    // Data-only mode: Chỉ trả về data cho caller
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in CreateButton_Click: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ShowError($"Lỗi khi tạo chương trình: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            if (ErrorMessageBlock != null)
            {
                ErrorMessageBlock.Text = message;
                ErrorMessageBlock.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}