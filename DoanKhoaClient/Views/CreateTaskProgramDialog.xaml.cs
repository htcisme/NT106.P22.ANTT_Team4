using DoanKhoaClient.Models;
using System;
using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class CreateTaskProgramDialog : Window
    {
        public TaskProgram TaskProgram { get; private set; }

        public CreateTaskProgramDialog(string sessionId, ProgramType type)
        {
            InitializeComponent();
            TaskProgram = new TaskProgram
            {
                SessionId = sessionId,
                Type = type,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            DataContext = TaskProgram;

            // Đặt tiêu đề dựa vào loại
            switch (type)
            {
                case ProgramType.Event:
                    this.Title = "Tạo chương trình sự kiện mới";
                    break;
                case ProgramType.Study:
                    this.Title = "Tạo chương trình học tập mới";
                    break;
                case ProgramType.Design:
                    this.Title = "Tạo chương trình thiết kế mới";
                    break;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
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