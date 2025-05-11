using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class EditTaskItemDialog : Window
    {
        public TaskItem TaskItem { get; private set; }
        private List<User> _users;
        private readonly TaskItem _originalTaskItem;

        public EditTaskItemDialog(TaskItem taskItem)
        {
            InitializeComponent();
            _originalTaskItem = taskItem;

            // Tạo bản sao để tránh thay đổi trực tiếp đến đối tượng gốc
            TaskItem = new TaskItem
            {
                Id = taskItem.Id,
                ProgramId = taskItem.ProgramId,
                Title = taskItem.Title,
                Description = taskItem.Description,
                Status = taskItem.Status,
                DueDate = taskItem.DueDate,
                CompletedAt = taskItem.CompletedAt,
                AssignedToId = taskItem.AssignedToId,
                AssignedToName = taskItem.AssignedToName,
                CreatedAt = taskItem.CreatedAt,
                UpdatedAt = DateTime.Now
            };

            DataContext = TaskItem;

            // Khởi tạo ComboBox trạng thái
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(TaskItemStatus));
            StatusComboBox.SelectedItem = TaskItem.Status;

            // Tải danh sách người dùng
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                // Sử dụng HttpClient để lấy danh sách người dùng
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.BaseAddress = new Uri("http://localhost:5299/api/");
                var response = await httpClient.GetAsync("user/all");
                if (response.IsSuccessStatusCode)
                {
                    _users = await response.Content.ReadFromJsonAsync<List<User>>();
                    AssigneeComboBox.ItemsSource = _users;
                    AssigneeComboBox.DisplayMemberPath = "DisplayName";

                    // Chọn người dùng hiện tại
                    if (!string.IsNullOrEmpty(TaskItem.AssignedToId))
                    {
                        var selectedUser = _users.Find(u => u.Id == TaskItem.AssignedToId);
                        if (selectedUser != null)
                        {
                            AssigneeComboBox.SelectedItem = selectedUser;
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể tải danh sách người dùng: {errorContent}",
                                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách người dùng: {ex.Message}",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                // Trong SaveButton_Click:
                if (StatusComboBox.SelectedItem is TaskItemStatus selectedStatus)
                {
                    TaskItem.Status = selectedStatus;
                }

                // Lưu người được giao từ ComboBox
                if (AssigneeComboBox.SelectedItem is User selectedUser)
                {
                    TaskItem.AssignedToId = selectedUser.Id;
                    TaskItem.AssignedToName = selectedUser.DisplayName;
                }

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
            if (string.IsNullOrWhiteSpace(TaskItem.Title))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề công việc.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TaskItem.Description))
            {
                MessageBox.Show("Vui lòng nhập mô tả công việc.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (AssigneeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn người được giao công việc.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}