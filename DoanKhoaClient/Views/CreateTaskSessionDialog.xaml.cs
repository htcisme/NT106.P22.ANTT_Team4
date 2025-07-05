using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public partial class CreateTaskSessionDialog : Window, INotifyPropertyChanged
    {
        private TaskSession _taskSession;

        public TaskSession TaskSession
        {
            get => _taskSession;
            set
            {
                _taskSession = value;
                OnPropertyChanged();
            }
        }

        public CreateTaskSessionDialog()
        {
            InitializeComponent();

            // Khởi tạo TaskSession mới với giá trị mặc định
            TaskSession = new TaskSession
            {
                Name = "",
                Type = TaskSessionType.Event, // Mặc định là Event
                ManagerName = GetCurrentUserName(),
                ManagerId = GetCurrentUserId(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            DataContext = this;

            // SỬA LẠI: Sử dụng Loaded event để đảm bảo controls đã được tạo
            this.Loaded += CreateTaskSessionDialog_Loaded;
        }

        private void CreateTaskSessionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up event handlers sau khi dialog đã load
            TypeComboBox.SelectionChanged += TypeComboBox_SelectionChanged;

            // Set default selection
            TypeComboBox.SelectedIndex = 0; // Event
            UpdateTypeDescription(TaskSessionType.Event);
        }

        private string GetCurrentUserName()
        {
            try
            {
                if (App.Current.Properties.Contains("CurrentUser"))
                {
                    var currentUser = (User)App.Current.Properties["CurrentUser"];
                    return currentUser.DisplayName ?? currentUser.Username ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user: {ex.Message}");
            }
            return "Unknown Manager";
        }

        private string GetCurrentUserId()
        {
            try
            {
                if (App.Current.Properties.Contains("CurrentUser"))
                {
                    var currentUser = (User)App.Current.Properties["CurrentUser"];
                    return currentUser.Id ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user ID: {ex.Message}");
            }
            return "";
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                var selectedType = (TaskSessionType)selectedItem.Tag;
                TaskSession.Type = selectedType;
                UpdateTypeDescription(selectedType);

                // Debug log
                System.Diagnostics.Debug.WriteLine($"Type changed to: {selectedType}");
            }
        }

        private void UpdateTypeDescription(TaskSessionType type)
        {
            // Đảm bảo TypeDescription đã được khởi tạo
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

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(TaskSession.Name))
            {
                MessageBox.Show("Vui lòng nhập tên phiên làm việc.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TaskSession.ManagerName))
            {
                MessageBox.Show("Vui lòng nhập tên người quản lý.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ManagerNameTextBox.Focus();
                return;
            }

            // Cập nhật thông tin cuối cùng
            TaskSession.CreatedAt = DateTime.Now;
            TaskSession.UpdatedAt = DateTime.Now;

            // Debug log
            System.Diagnostics.Debug.WriteLine($"Creating TaskSession: Name={TaskSession.Name}, Type={TaskSession.Type}, ManagerName={TaskSession.ManagerName}");

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}