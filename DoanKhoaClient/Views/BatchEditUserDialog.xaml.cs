using DoanKhoaClient.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace DoanKhoaClient.Views
{
    /// <summary>
    /// Interaction logic for BatchEditUserDialog.xaml
    /// </summary>
    public partial class BatchEditUserDialog : Window, INotifyPropertyChanged
    {
        private BatchEditOptions _editOptions;

        public BatchEditOptions EditOptions
        {
            get => _editOptions;
            set
            {
                _editOptions = value;
                OnPropertyChanged();
            }
        }

        public BatchEditUserDialog()
        {
            InitializeComponent();
            EditOptions = new BatchEditOptions();
            EditOptions.PropertyChanged += EditOptions_PropertyChanged;
            DataContext = this;
        }

        private void EditOptions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BatchEditOptions.Position))
            {
                if (EditOptions.Position == Position.None)
                {
                    EditOptions.Position = null;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem có ít nhất một trường được chọn để cập nhật không
            if (!EditOptions.UpdateRole && !EditOptions.UpdateStatus && !EditOptions.UpdatePosition)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một trường để cập nhật.",
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kiểm tra các trường bắt buộc cho mỗi tùy chọn
            if (EditOptions.UpdateRole && !EditOptions.Role.HasValue)
            {
                MessageBox.Show("Vui lòng chọn vai trò cần cập nhật.",
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditOptions.UpdateStatus && !EditOptions.IsActive.HasValue)
            {
                MessageBox.Show("Vui lòng chọn trạng thái cần cập nhật.",
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditOptions.UpdatePosition && !EditOptions.Position.HasValue)
            {
                MessageBox.Show("Vui lòng chọn chức vụ cần cập nhật.",
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

    // Lớp chứa các tùy chọn chỉnh sửa hàng loạt
    public class BatchEditOptions : INotifyPropertyChanged
    {
        private bool _updateRole;
        private UserRole? _role;
        private bool _updateStatus;
        private bool? _isActive;
        private bool _updatePosition;
        private Position? _position;

        public bool UpdateRole
        {
            get => _updateRole;
            set
            {
                _updateRole = value;
                OnPropertyChanged();
            }
        }

        public UserRole? Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        public bool UpdateStatus
        {
            get => _updateStatus;
            set
            {
                _updateStatus = value;
                OnPropertyChanged();
            }
        }

        public bool? IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public bool UpdatePosition
        {
            get => _updatePosition;
            set
            {
                _updatePosition = value;
                OnPropertyChanged();
            }
        }

        public Position? Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}