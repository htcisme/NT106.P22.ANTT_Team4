using System;
using System.Windows;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class EditTaskProgramDialog : Window
    {
        private readonly TaskSession _session;
        private readonly TaskService _taskService;
        // private List<User> _users; // Không cần thiết nữa

        public TaskProgram Program { get; set; }

        public EditTaskProgramDialog(TaskSession session, TaskProgram program)
        {
            InitializeComponent();
            _session = session;
            _taskService = new TaskService();
            Program = program;

            DataContext = Program;

            // Bỏ việc gọi LoadUsers() vì không cần nữa
            // LoadUsers();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kiểm tra đầu vào
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

                // Cập nhật dữ liệu từ UI vào đối tượng Program
                Program.Name = ProgramNameTextBox.Text.Trim();
                Program.Description = DescriptionTextBox.Text.Trim();
                Program.StartDate = StartDatePicker.SelectedDate.Value;
                Program.EndDate = EndDatePicker.SelectedDate.Value;

                // Giữ nguyên thông tin người thực hiện
                // Không lấy từ UI nữa vì đã bỏ phần UI tương ứng
                // Nếu không có sẵn, gán giá trị mặc định
                if (string.IsNullOrEmpty(Program.ExecutorId))
                {
                    Program.ExecutorId = _session.Id;
                }
                if (string.IsNullOrEmpty(Program.ExecutorName))
                {
                    Program.ExecutorName = "Auto Assigned";
                }

                // Đảm bảo ID luôn có giá trị
                if (string.IsNullOrEmpty(Program.Id))
                {
                    Program.Id = Guid.NewGuid().ToString();
                }

                // Gọi API để cập nhật
                var updatedProgram = await _taskService.UpdateTaskProgramAsync(Program.Id, Program);
                Program = updatedProgram;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi cập nhật chương trình: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageBlock.Text = message;
            ErrorMessageBlock.Visibility = Visibility.Visible;
        }
    }
}