using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                var response = await _httpClient.GetAsync("users");
                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<User>>();

                    // Filter out current user
                    var currentViewModel = (Application.Current.MainWindow.DataContext as LightUserChatViewModel);
                    var currentUserId = currentViewModel?.CurrentUser?.Id;

                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        users = users.Where(u => u.Id != currentUserId).ToList();
                    }

                    UsersListView.ItemsSource = users;
                }
                else
                {
                    // Load demo users if API call fails
                    var demoUsers = new List<User>
                    {
                        new User { Id = "2", DisplayName = "Nguyen Van A" },
                        new User { Id = "3", DisplayName = "Tran Thi B" },
                        new User { Id = "4", DisplayName = "Le Van C" }
                    };

                    UsersListView.ItemsSource = demoUsers;
                }
            }
            catch
            {
                // Load demo users if exception occurs
                var demoUsers = new List<User>
                {
                    new User { Id = "2", DisplayName = "Nguyen Van A" },
                    new User { Id = "3", DisplayName = "Tran Thi B" },
                    new User { Id = "4", DisplayName = "Le Van C" }
                };

                UsersListView.ItemsSource = demoUsers;
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
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}