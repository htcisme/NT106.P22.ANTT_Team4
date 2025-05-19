using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DoanKhoaClient.Helpers;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.ViewModels
{
    public class AddUserViewModel : INotifyPropertyChanged
    {
        private User _user;
        private string _passwordError;
        private string _adminCodeError;
        private bool _showAdminCode;

        public User User
        {
            get => _user;
            set
            {
                _user = value;
                OnPropertyChanged();
            }
        }

        public string PasswordError
        {
            get => _passwordError;
            set
            {
                _passwordError = value;
                OnPropertyChanged();
            }
        }

        public string AdminCodeError
        {
            get => _adminCodeError;
            set
            {
                _adminCodeError = value;
                OnPropertyChanged();
            }
        }

        public bool ShowAdminCode
        {
            get => _showAdminCode;
            set
            {
                _showAdminCode = value;
                OnPropertyChanged();
            }
        }

        public ICommand PasswordChangedCommand { get; set; }
        public ICommand AdminCodeChangedCommand { get; set; }
        public ICommand RoleChangedCommand { get; private set; }

        public AddUserViewModel()
        {
            User = new User();
            RoleChangedCommand = new RelayCommand(ExecuteRoleChanged);
        }

        private void ExecuteRoleChanged(object parameter)
        {
            if (User != null)
            {
                ShowAdminCode = User.Role == UserRole.Admin;
                // Reset admin code error when changing role
                if (!ShowAdminCode)
                    AdminCodeError = string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
