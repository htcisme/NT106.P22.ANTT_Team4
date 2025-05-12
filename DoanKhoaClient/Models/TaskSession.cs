using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoanKhoaClient.Models
{
    public class TaskSession : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _managerId;
        private string _managerName;
        private TaskSessionType _type;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string ManagerId
        {
            get => _managerId;
            set { _managerId = value; OnPropertyChanged(); }
        }

        public string ManagerName
        {
            get => _managerName;
            set { _managerName = value; OnPropertyChanged(); }
        }

        public TaskSessionType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
            public void PrepareForSending()
        {
            // Đảm bảo Id là null để server tạo mới
            Id = "000000000000000000000000";  // 24 chữ số 0

            // Đảm bảo timestamps
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

    }


    public enum TaskSessionType
    {
        Event,
        Study,
        Design
    }
}