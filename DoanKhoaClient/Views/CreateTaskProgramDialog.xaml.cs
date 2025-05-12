using System;
using System.Collections.Generic;
using System.Windows;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class CreateTaskProgramDialog : Window
    {
        private readonly TaskSession _session;
        private readonly TaskService _taskService;
        // private List<User> _users; // Không cần thiết nữa

        public TaskProgram ProgramToCreate { get; set; }

        public CreateTaskProgramDialog(TaskSession session)
        {
            InitializeComponent();
            _session = session;
            _taskService = new TaskService();

            ProgramToCreate = new TaskProgram
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7)
            };

            DataContext = ProgramToCreate;

            // Khởi tạo DatePicker với ngày hiện tại nếu binding không hoạt động
            if (StartDatePicker.SelectedDate == null)
                StartDatePicker.SelectedDate = DateTime.Today;

            if (EndDatePicker.SelectedDate == null)
                EndDatePicker.SelectedDate = DateTime.Today.AddDays(7);

            // Bỏ việc gọi LoadUsers() vì không cần nữa
            // LoadUsers();
        }

        // Có thể xóa hoặc giữ lại phương thức LoadUsers() nhưng không gọi nó

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
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

                // Cập nhật các trường khác nếu binding không hoạt động
                ProgramToCreate.Name = ProgramNameTextBox.Text.Trim();
                ProgramToCreate.Description = DescriptionTextBox.Text.Trim();
                ProgramToCreate.StartDate = StartDatePicker.SelectedDate.Value;
                ProgramToCreate.EndDate = EndDatePicker.SelectedDate.Value;
                ProgramToCreate.SessionId = _session.Id;

                // Thêm thông tin người thực hiện là người hiện tại đang đăng nhập
                ProgramToCreate.ExecutorId = _session.Id; // Sử dụng ID của phiên làm việc
                ProgramToCreate.ExecutorName = "Auto Assigned"; // Tên mặc định


                // Gọi API để tạo mới
                var createdProgram = await _taskService.CreateTaskProgramAsync(ProgramToCreate);
                ProgramToCreate = createdProgram; // Cập nhật lại với ID mới

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi tạo chương trình: {ex.Message}");
            }
        }
        private void ShowError(string message)
        {
            ErrorMessageBlock.Text = message;
            ErrorMessageBlock.Visibility = Visibility.Visible;
        }
    }
}