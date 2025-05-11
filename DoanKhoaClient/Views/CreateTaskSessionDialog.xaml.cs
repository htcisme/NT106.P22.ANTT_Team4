using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class CreateTaskSessionDialog : Window
    {
        public TaskSession TaskSession { get; private set; }
        private List<User> _users;

        public CreateTaskSessionDialog()
        {
            InitializeComponent();
            TaskSession = new TaskSession
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Type = TaskSessionType.Event // Giá trị mặc định
            };
            DataContext = TaskSession;

            // Khởi tạo TypeComboBox
            TypeComboBox.ItemsSource = Enum.GetValues(typeof(TaskSessionType));
            TypeComboBox.SelectedItem = TaskSession.Type;

            // Tải danh sách người dùng
            _ = LoadUsersAsync();
        }


        private async Task LoadUsersAsync()
        {
            try
            {
                // Lấy thông tin người dùng hiện tại từ App.Current.Properties
                var currentUser = App.Current.Properties["CurrentUser"] as User;
                if (currentUser == null)
                {
                    MessageBox.Show("Không thể lấy thông tin người dùng.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Sử dụng HttpClient để lấy danh sách người dùng
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.BaseAddress = new Uri("http://localhost:5299/api/");
                var response = await httpClient.GetAsync("user/all");
                if (response.IsSuccessStatusCode)
                {
                    _users = await response.Content.ReadFromJsonAsync<List<User>>();
                    ManagerComboBox.ItemsSource = _users;
                    ManagerComboBox.DisplayMemberPath = "DisplayName";
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

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                // Lưu manager từ ComboBox
                if (ManagerComboBox.SelectedItem is User selectedUser)
                {
                    TaskSession.ManagerId = selectedUser.Id;
                    TaskSession.ManagerName = selectedUser.DisplayName;
                }

                // Lưu type từ ComboBox
                if (TypeComboBox.SelectedItem is TaskSessionType selectedType)
                {
                    TaskSession.Type = selectedType;
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