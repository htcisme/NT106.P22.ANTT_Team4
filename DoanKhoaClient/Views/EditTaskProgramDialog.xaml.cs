using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DoanKhoaClient.Models;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class EditTaskProgramDialog : Window
    {
        public TaskProgram TaskProgram { get; private set; }
        private List<User> _users;

        public EditTaskProgramDialog(TaskProgram taskProgram)
        {
            InitializeComponent();
            
            // Tạo bản sao để tránh thay đổi trực tiếp đối tượng gốc
            TaskProgram = new TaskProgram
            {
                Id = taskProgram.Id,
                SessionId = taskProgram.SessionId,
                Name = taskProgram.Name,
                Description = taskProgram.Description,
                Type = taskProgram.Type,
                StartDate = taskProgram.StartDate,
                EndDate = taskProgram.EndDate,
                ExecutorId = taskProgram.ExecutorId,
                ExecutorName = taskProgram.ExecutorName,
                CreatedAt = taskProgram.CreatedAt,
                UpdatedAt = DateTime.Now
            };
            
            DataContext = TaskProgram;
            
            // Đặt giá trị cho các trường nếu binding không hoạt động
            ProgramNameTextBox.Text = TaskProgram.Name;
            DescriptionTextBox.Text = TaskProgram.Description;
            StartDatePicker.SelectedDate = TaskProgram.StartDate;
            EndDatePicker.SelectedDate = TaskProgram.EndDate;
            
            // Tải danh sách người dùng
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var userService = new UserService();
                _users = await userService.GetUsersAsync();
                ExecutorComboBox.ItemsSource = _users;
                ExecutorComboBox.DisplayMemberPath = "DisplayName";
                ExecutorComboBox.SelectedValuePath = "Id";
                
                // Chọn người thực hiện hiện tại
                if (!string.IsNullOrEmpty(TaskProgram.ExecutorId))
                {
                    var selectedUser = _users.FirstOrDefault(u => u.Id == TaskProgram.ExecutorId);
                    if (selectedUser != null)
                    {
                        ExecutorComboBox.SelectedItem = selectedUser;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách người dùng: {ex.Message}",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                // Cập nhật thông tin từ UI
                TaskProgram.Name = ProgramNameTextBox.Text.Trim();
                TaskProgram.Description = DescriptionTextBox.Text.Trim();
                TaskProgram.StartDate = StartDatePicker.SelectedDate ?? DateTime.Today;
                TaskProgram.EndDate = EndDatePicker.SelectedDate ?? DateTime.Today.AddDays(7);
                
                // Cập nhật thông tin người thực hiện
                if (ExecutorComboBox.SelectedItem is User selectedUser)
                {
                    TaskProgram.ExecutorId = selectedUser.Id;
                    TaskProgram.ExecutorName = selectedUser.DisplayName;
                }
                
                DialogResult = true;
                Close();
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(ProgramNameTextBox.Text))
            {
                ShowError("Vui lòng nhập tên chương trình");
                return false;
            }

            if (ExecutorComboBox.SelectedItem == null)
            {
                ShowError("Vui lòng chọn người thực hiện");
                return false;
            }

            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                ShowError("Vui lòng chọn ngày bắt đầu và kết thúc");
                return false;
            }

            if (EndDatePicker.SelectedDate < StartDatePicker.SelectedDate)
            {
                ShowError("Ngày kết thúc phải sau ngày bắt đầu");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            ErrorMessageBlock.Text = message;
            ErrorMessageBlock.Visibility = Visibility.Visible;
        }
    }
}