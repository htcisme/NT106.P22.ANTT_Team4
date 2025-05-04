using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace DoanKhoaClient.Views
{
    public partial class CreateGroupDialog : Window
    {
        private readonly HttpClient _httpClient;
        public string GroupName { get; private set; }
        public List<User> SelectedUsers { get; private set; }
        public bool DialogConfirmed { get; private set; }

        public CreateGroupDialog()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };
            SelectedUsers = new List<User>();
            Loaded += CreateGroupDialog_Loaded;
        }

        private async void CreateGroupDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            try
            {
                // Get the current user
                var currentUser = App.Current.Properties["CurrentUser"] as User;
                if (currentUser == null)
                {
                    MessageBox.Show("Không thể lấy thông tin người dùng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get all users from server
                var response = await _httpClient.GetAsync("user/all");
                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<User>>();

                    // Remove current user from the list
                    users = users.Where(u => u.Id != currentUser.Id).ToList();

                    // Bind to the ListView
                    UsersListView.ItemsSource = users;
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
            if (string.IsNullOrWhiteSpace(GroupNameTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập tên nhóm", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UsersListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một thành viên", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GroupName = GroupNameTextBox.Text;
            SelectedUsers = UsersListView.SelectedItems.Cast<User>().ToList();
            DialogConfirmed = true;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}