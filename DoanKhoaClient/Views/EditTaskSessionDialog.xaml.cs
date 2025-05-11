using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class EditTaskSessionDialog : Window
    {
        public TaskSession TaskSession { get; private set; }
        private List<User> _users;
        private readonly TaskSession _originalTaskSession;

        public EditTaskSessionDialog(TaskSession taskSession)
        {
            InitializeComponent();
            _originalTaskSession = taskSession;
            
            // Tạo bản sao để tránh thay đổi trực tiếp đến đối tượng gốc
            TaskSession = new TaskSession
            {
                Id = taskSession.Id,
                Name = taskSession.Name,
                ManagerId = taskSession.ManagerId,
                ManagerName = taskSession.ManagerName,
                CreatedAt = taskSession.CreatedAt,
                UpdatedAt = DateTime.Now
            };
            
            DataContext = TaskSession;

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
                    ManagerComboBox.ItemsSource = _users;
                    ManagerComboBox.DisplayMemberPath = "DisplayName";
                    
                    // Chọn manager hiện tại
                    if (!string.IsNullOrEmpty(TaskSession.ManagerId))
                    {
                        var selectedManager = _users.Find(u => u.Id == TaskSession.ManagerId);
                        if (selectedManager != null)
                        {
                            ManagerComboBox.SelectedItem = selectedManager;
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
                // Lưu manager từ ComboBox
                if (ManagerComboBox.SelectedItem is User selectedUser)
                {
                    TaskSession.ManagerId = selectedUser.Id;
                    TaskSession.ManagerName = selectedUser.DisplayName;
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
            if (string.IsNullOrWhiteSpace(TaskSession.Name))
            {
                MessageBox.Show("Vui lòng nhập tên phiên làm việc.", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (ManagerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn người quản lý.", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}