using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics; 

namespace DoanKhoaClient.Views
{
    public partial class EditTaskItemDialog : Window
    {
        private readonly UserService _userService;
        private List<User> _users;

        public TaskItem TaskItem { get; private set; }

        public EditTaskItemDialog(TaskItem taskItem)
        {
            InitializeComponent();
            _userService = new UserService();

            // Tạo bản sao để tránh sửa trực tiếp
            TaskItem = new TaskItem
            {
                Id = taskItem.Id,
                ProgramId = taskItem.ProgramId,
                Title = taskItem.Title,
                Description = taskItem.Description,
                Status = taskItem.Status,
                Priority = taskItem.Priority,
                DueDate = taskItem.DueDate,
                CompletedAt = taskItem.CompletedAt,
                AssignedToId = taskItem.AssignedToId,
                AssignedToName = taskItem.AssignedToName,
                CreatedAt = taskItem.CreatedAt,
                UpdatedAt = DateTime.Now
            };

            DataContext = TaskItem;

            // Khởi tạo danh sách trạng thái
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(TaskItemStatus));
            StatusComboBox.SelectedItem = TaskItem.Status;

            // Tải danh sách người dùng
            Loaded += async (s, e) => await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                _users = await _userService.GetUsersAsync();

                // Thêm tùy chọn "Không có người được giao"
                var noneItem = new ComboBoxItem
                {
                    Content = "Không có người được giao",
                    Tag = null
                };
                AssigneeComboBox.Items.Add(noneItem);

                // Thêm danh sách người dùng
                foreach (var user in _users)
                {
                    var item = new ComboBoxItem
                    {
                        Content = user.DisplayName,
                        Tag = user
                    };
                    AssigneeComboBox.Items.Add(item);
                }

                // Chọn người dùng hiện tại hoặc "Không có người được giao"
                if (string.IsNullOrEmpty(TaskItem.AssignedToId))
                {
                    AssigneeComboBox.SelectedIndex = 0;
                }
                else
                {
                    var userIndex = _users.FindIndex(u => u.Id == TaskItem.AssignedToId);
                    if (userIndex >= 0)
                    {
                        AssigneeComboBox.SelectedIndex = userIndex + 1; // +1 vì có item "Không có người được giao"
                    }
                    else
                    {
                        AssigneeComboBox.SelectedIndex = 0;
                    }
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
                // Đảm bảo các trường quan trọng không bị null
                if (string.IsNullOrEmpty(TaskItem.Id))
                {
                    MessageBox.Show("Lỗi: Task ID không được để trống",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(TaskItem.ProgramId))
                {
                    MessageBox.Show("Lỗi: Program ID không được để trống",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Cập nhật từ control
                TaskItem.Status = (TaskItemStatus)StatusComboBox.SelectedItem;
                TaskItem.UpdatedAt = DateTime.Now;

                // Xử lý người được giao
                if (AssigneeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var selectedUser = selectedItem.Tag as User;
                    if (selectedUser != null)
                    {
                        TaskItem.AssignedToId = selectedUser.Id;
                        TaskItem.AssignedToName = selectedUser.DisplayName;
                    }
                    else if (AssigneeComboBox.SelectedIndex == 0)
                    {
                        TaskItem.AssignedToId = null;
                        TaskItem.AssignedToName = null;
                    }
                }

                // Log task item trước khi gửi để debug
                Debug.WriteLine($"Saving task: {TaskItem.Id}");
                Debug.WriteLine($"Title: {TaskItem.Title}");
                Debug.WriteLine($"ProgramId: {TaskItem.ProgramId}");
                Debug.WriteLine($"Status: {TaskItem.Status}");

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
                MessageBox.Show("Vui lòng nhập tiêu đề công việc.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TaskItem.DueDate.HasValue)
            {
                MessageBox.Show("Vui lòng chọn ngày hạn cho công việc.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}