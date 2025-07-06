using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DoanKhoaClient.Views
{
    public partial class CreateTaskItemDialog : Window
    {
        public TaskItem TaskItem { get; private set; }
        private List<User> _users;
        private string _programId;

        public CreateTaskItemDialog(string programId)
        {
            InitializeComponent();
            _programId = programId;

            TaskItem = new TaskItem
            {
                ProgramId = programId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = TaskItemStatus.NotStarted,
                Priority = TaskPriority.Medium
            };
            DataContext = TaskItem;

            InitializeComboBoxes();
            _ = LoadUsersAsync();
        }

        private void InitializeComboBoxes()
        {
            // Initialize Status ComboBox
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
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.BaseAddress = new Uri("http://localhost:5299/api/");
                var response = await httpClient.GetAsync("user/all");

                if (response.IsSuccessStatusCode)
                {
                    _users = await response.Content.ReadFromJsonAsync<List<User>>();

                    // Thêm option "Nhập thủ công"
                    var items = new List<object>();
                    items.Add(new { DisplayName = "Nhập thủ công", Id = "manual", Email = "" });

                    foreach (var user in _users)
                    {
                        items.Add(user);
                    }

                    AssigneeComboBox.ItemsSource = items;
                    AssigneeComboBox.DisplayMemberPath = "DisplayName";
                    AssigneeComboBox.SelectedIndex = 0; // Default to "Nhập thủ công"
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

        private void AssigneeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssigneeComboBox.SelectedItem != null)
            {
                var selectedItem = AssigneeComboBox.SelectedItem;

                // Sử dụng reflection để lấy properties
                var idProperty = selectedItem.GetType().GetProperty("Id");
                var emailProperty = selectedItem.GetType().GetProperty("Email");
                var displayNameProperty = selectedItem.GetType().GetProperty("DisplayName");

                var id = idProperty?.GetValue(selectedItem)?.ToString();
                var email = emailProperty?.GetValue(selectedItem)?.ToString();
                var displayName = displayNameProperty?.GetValue(selectedItem)?.ToString();

                if (id == "manual")
                {
                    // Nhập thủ công - clear email field
                    AssigneeEmailTextBox.Text = "";
                    AssigneeEmailTextBox.IsEnabled = true;
                    AssigneeEmailTextBox.Focus();
                }
                else if (selectedItem is User user)
                {
                    // User được chọn - auto fill email
                    AssigneeEmailTextBox.Text = user.Email ?? GenerateEmailFromName(user.DisplayName);
                    AssigneeEmailTextBox.IsEnabled = true; // Vẫn cho phép chỉnh sửa
                    EmailPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void AssigneeEmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrEmpty(AssigneeEmailTextBox.Text) ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                // Cập nhật TaskItem với dữ liệu từ form
                TaskItem.Status = (TaskItemStatus)StatusComboBox.SelectedItem;
                TaskItem.Priority = (TaskPriority)PriorityComboBox.SelectedValue;

                // Xử lý assignee
                var selectedItem = AssigneeComboBox.SelectedItem;
                if (selectedItem != null)
                {
                    var idProperty = selectedItem.GetType().GetProperty("Id");
                    var displayNameProperty = selectedItem.GetType().GetProperty("DisplayName");

                    var id = idProperty?.GetValue(selectedItem)?.ToString();
                    var displayName = displayNameProperty?.GetValue(selectedItem)?.ToString();

                    if (id != "manual" && selectedItem is User user)
                    {
                        TaskItem.AssignedToId = user.Id;
                        TaskItem.AssignedToName = user.DisplayName;
                    }
                    else
                    {
                        // Manual input
                        TaskItem.AssignedToId = null;
                        TaskItem.AssignedToName = ExtractNameFromEmail(AssigneeEmailTextBox.Text.Trim());
                    }
                }

                // QUAN TRỌNG: Set AssignedToEmail cho server
                TaskItem.AssignedToEmail = AssigneeEmailTextBox.Text.Trim();

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
                MessageBox.Show("Vui lòng nhập tiêu đề công việc.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TaskItem.Description))
            {
                MessageBox.Show("Vui lòng nhập mô tả công việc.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (AssigneeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn người được giao công việc.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show("Email không hợp lệ. Vui lòng nhập email đúng định dạng.\n\nVí dụ: user@domain.com",
                    "Email không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                AssigneeEmailTextBox.Focus();
                AssigneeEmailTextBox.SelectAll();
                return false;
            }

            if (!TaskItem.DueDate.HasValue)
            {
                MessageBox.Show("Vui lòng chọn ngày hạn cho công việc.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // Hardcoded mappings
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

                // Generate từ tên
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