using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DoanKhoaClient.Views
{
    public partial class EditTaskSessionDialog : Window, INotifyPropertyChanged
    {
        private TaskSession _taskSession;
        private List<User> _users;
        private readonly TaskSession _originalTaskSession;

        public TaskSession TaskSession
        {
            get => _taskSession;
            set
            {
                _taskSession = value;
                OnPropertyChanged();
            }
        }

        public EditTaskSessionDialog(TaskSession taskSession)
        {
            InitializeComponent();
            _originalTaskSession = taskSession;

            // Tạo bản sao để tránh thay đổi trực tiếp đến đối tượng gốc
            TaskSession = new TaskSession
            {
                Id = taskSession.Id,
                Name = taskSession.Name,
                Type = taskSession.Type, // THÊM Type
                ManagerId = taskSession.ManagerId,
                ManagerName = taskSession.ManagerName,
                CreatedAt = taskSession.CreatedAt,
                UpdatedAt = DateTime.Now
            };

            DataContext = this;

            // Setup sau khi load
            this.Loaded += EditTaskSessionDialog_Loaded;
        }

        private void EditTaskSessionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Setup TypeComboBox
            TypeComboBox.SelectionChanged += TypeComboBox_SelectionChanged;

            // Set current type
            SetCurrentType(TaskSession.Type);

            // Load users
            _ = LoadUsersAsync();
        }

        private void SetCurrentType(TaskSessionType type)
        {
            switch (type)
            {
                case TaskSessionType.Event:
                    TypeComboBox.SelectedIndex = 0;
                    break;
                case TaskSessionType.Study:
                    TypeComboBox.SelectedIndex = 1;
                    break;
                case TaskSessionType.Design:
                    TypeComboBox.SelectedIndex = 2;
                    break;
            }
            UpdateTypeDescription(type);
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                var selectedType = (TaskSessionType)selectedItem.Tag;
                TaskSession.Type = selectedType;
                UpdateTypeDescription(selectedType);
            }
        }

        private void UpdateTypeDescription(TaskSessionType type)
        {
            if (TypeDescription == null) return;

            switch (type)
            {
                case TaskSessionType.Event:
                    TypeDescription.Text = "Phiên làm việc dành cho tổ chức các sự kiện, hội thảo, workshop và các hoạt động cộng đồng.";
                    break;
                case TaskSessionType.Study:
                    TypeDescription.Text = "Phiên làm việc dành cho các hoạt động học tập, nghiên cứu, đào tạo và phát triển kỹ năng.";
                    break;
                case TaskSessionType.Design:
                    TypeDescription.Text = "Phiên làm việc dành cho các dự án thiết kế, sáng tạo nội dung và phát triển sản phẩm.";
                    break;
                default:
                    TypeDescription.Text = "Chọn loại phiên làm việc để xem mô tả chi tiết.";
                    break;
            }
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

                // Đảm bảo cập nhật thời gian
                TaskSession.UpdatedAt = DateTime.Now;

                // Debug log
                System.Diagnostics.Debug.WriteLine($"Updating TaskSession: Name={TaskSession.Name}, Type={TaskSession.Type}, ManagerName={TaskSession.ManagerName}");

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
                NameTextBox.Focus();
                return false;
            }

            if (ManagerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn người quản lý.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ManagerComboBox.Focus();
                return false;
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}