using DoanKhoaClient.Models;
using System;
using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class EditTaskProgramDialog : Window
    {
        public TaskProgram TaskProgram { get; private set; }

        public EditTaskProgramDialog(TaskProgram taskProgram)
        {
            InitializeComponent();
            TaskProgram = new TaskProgram
            {
                Id = taskProgram.Id,
                SessionId = taskProgram.SessionId,
                Name = taskProgram.Name,
                Description = taskProgram.Description,
                Type = taskProgram.Type,
                StartDate = taskProgram.StartDate,
                EndDate = taskProgram.EndDate,
                CreatedAt = taskProgram.CreatedAt,
                UpdatedAt = DateTime.Now
            };
            DataContext = TaskProgram;

            // Đặt tiêu đề dựa vào loại
            switch (taskProgram.Type)
            {
                case ProgramType.Event:
                    this.Title = "Chỉnh sửa chương trình sự kiện";
                    break;
                case ProgramType.Study:
                    this.Title = "Chỉnh sửa chương trình học tập";
                    break;
                case ProgramType.Design:
                    this.Title = "Chỉnh sửa chương trình thiết kế";
                    break;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TaskProgram.Name))
            {
                MessageBox.Show("Vui lòng nhập tên chương trình.", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TaskProgram.Description))
            {
                MessageBox.Show("Vui lòng nhập mô tả chương trình.", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (TaskProgram.EndDate < TaskProgram.StartDate)
            {
                MessageBox.Show("Ngày kết thúc phải sau ngày bắt đầu.", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}