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
        private TaskItem _originalTaskItem;

        public EditTaskItemDialog(TaskItem taskItem)
        {
            InitializeComponent();
            _userService = new UserService();
            _originalTaskItem = taskItem;

            // SỬA: Sử dụng Clone method thay vì tạo manual
            TaskItem = taskItem.Clone();

            DataContext = TaskItem;
            InitializeComboBoxes();

            // Load email hiện tại
            AssigneeEmailTextBox.Text = TaskItem.AssignedToEmail ?? "";
            if (!string.IsNullOrEmpty(TaskItem.AssignedToEmail))
            {
                EmailPlaceholder.Visibility = Visibility.Collapsed;
            }

            // Tải danh sách người dùng
            Loaded += async (s, e) => await LoadUsersAsync();
        }

        private void InitializeComboBoxes()
        {
            // Khởi tạo danh sách trạng thái
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(TaskItemStatus));
            StatusComboBox.SelectedItem = TaskItem.Status;

            // Initialize Priority ComboBox
            PriorityComboBox.ItemsSource = new[]
            {
                new { Display = "Thấp", Value = TaskPriority.Low },
                new { Display = "Trung bình", Value = TaskPriority.Medium },
                new { Display = "Cao", Value = TaskPriority.High },
                new { Display = "Khẩn cấp", Value = TaskPriority.Critical }
            };
            PriorityComboBox.DisplayMemberPath = "Display";
            PriorityComboBox.SelectedValuePath = "Value";
            PriorityComboBox.SelectedValue = TaskItem.Priority;
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                _users = await _userService.GetUsersAsync();

                // Thêm tùy chọn "Nhập thủ công"
                var items = new List<object>();
                items.Add(new { DisplayName = "Nhập thủ công", Id = "manual", Email = "" });

                // Thêm danh sách người dùng
                foreach (var user in _users)
                {
                    items.Add(user);
                }

                AssigneeComboBox.ItemsSource = items;
                AssigneeComboBox.DisplayMemberPath = "DisplayName";

                // Chọn người dùng hiện tại
                if (string.IsNullOrEmpty(TaskItem.AssignedToId))
                {
                    AssigneeComboBox.SelectedIndex = 0; // "Nhập thủ công"
                }
                else
                {
                    var userIndex = _users.FindIndex(u => u.Id == TaskItem.AssignedToId);
                    if (userIndex >= 0)
                    {
                        AssigneeComboBox.SelectedIndex = userIndex + 1; // +1 vì có "Nhập thủ công"
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

        private void AssigneeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssigneeComboBox.SelectedItem != null)
            {
                var selectedItem = AssigneeComboBox.SelectedItem;

                // Check if it's manual input
                var idProperty = selectedItem.GetType().GetProperty("Id");
                var id = idProperty?.GetValue(selectedItem)?.ToString();

                if (id == "manual")
                {
                    // Keep current email, enable editing
                    AssigneeEmailTextBox.IsEnabled = true;
                    AssigneeEmailTextBox.Focus();
                }
                else if (selectedItem is User user)
                {
                    // Auto-fill email từ user
                    var newEmail = user.Email ?? GenerateEmailFromName(user.DisplayName);
                    if (!string.IsNullOrEmpty(newEmail))
                    {
                        AssigneeEmailTextBox.Text = newEmail;
                        EmailPlaceholder.Visibility = Visibility.Collapsed;
                    }
                    AssigneeEmailTextBox.IsEnabled = true;
                }
            }
        }

        private void AssigneeEmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrEmpty(AssigneeEmailTextBox.Text) ?
                Visibility.Visible : Visibility.Collapsed;
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

                // Cập nhật từ controls
                TaskItem.Status = (TaskItemStatus)StatusComboBox.SelectedItem;
                TaskItem.Priority = (TaskPriority)PriorityComboBox.SelectedValue;
                TaskItem.UpdatedAt = DateTime.Now;

                // QUAN TRỌNG: Cập nhật email
                TaskItem.AssignedToEmail = AssigneeEmailTextBox.Text.Trim();

                // Xử lý người được giao
                var selectedItem = AssigneeComboBox.SelectedItem;
                if (selectedItem != null)
                {
                    var idProperty = selectedItem.GetType().GetProperty("Id");
                    var id = idProperty?.GetValue(selectedItem)?.ToString();

                    if (id == "manual")
                    {
                        // Manual input
                        TaskItem.AssignedToId = null;
                        TaskItem.AssignedToName = ExtractNameFromEmail(AssigneeEmailTextBox.Text.Trim());
                    }
                    else if (selectedItem is User user)
                    {
                        TaskItem.AssignedToId = user.Id;
                        TaskItem.AssignedToName = user.DisplayName;
                    }
                }

                // Log để debug
                Debug.WriteLine($"Saving task: {TaskItem.Id}");
                Debug.WriteLine($"Title: {TaskItem.Title}");
                Debug.WriteLine($"ProgramId: {TaskItem.ProgramId}");
                Debug.WriteLine($"Status: {TaskItem.Status}");
                Debug.WriteLine($"AssignedToEmail: {TaskItem.AssignedToEmail}");

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

            // QUAN TRỌNG: Validate email
            if (string.IsNullOrWhiteSpace(AssigneeEmailTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập email người thực hiện.\nĐây là trường bắt buộc của server.",
                    "Thiếu email", MessageBoxButton.OK, MessageBoxImage.Error);
                AssigneeEmailTextBox.Focus();
                return false;
            }

            if (!IsValidEmail(AssigneeEmailTextBox.Text.Trim()))
            {
                MessageBox.Show("Email không hợp lệ. Vui lòng nhập email đúng định dạng.",
                    "Email không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                AssigneeEmailTextBox.Focus();
                AssigneeEmailTextBox.SelectAll();
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

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateEmailFromName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "";

            try
            {
                var knownEmails = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Trần Văn Nam", "tran.van.nam@test.com" },
                    { "Hoàng Bảo Phước", "hoang.bao.phuoc@test.com" },
                    { "Huỳnh Ngọc Ngân Tuyền", "huynh.ngan.tuyen@test.com" },
                    { "Test User", "test.user@test.com" },
                    { "Admin", "admin@test.com" }
                };

                if (knownEmails.TryGetValue(fullName, out var knownEmail))
                {
                    return knownEmail;
                }

                // Generate from name
                var normalized = fullName.ToLower()
                    .Replace(" ", ".")
                    .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                    .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                    .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                    .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                    .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                    .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                    .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                    .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                    .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                    .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                    .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                    .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                    .Replace("đ", "d");

                return $"{normalized}@doankhoa.uit.edu.vn";
            }
            catch
            {
                return $"{fullName.ToLower().Replace(" ", ".")}@test.com";
            }
        }

        private string ExtractNameFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "Người dùng";

            try
            {
                var localPart = email.Split('@')[0];
                return localPart.Replace(".", " ").Replace("_", " ");
            }
            catch
            {
                return "Người dùng";
            }
        }
    }
}