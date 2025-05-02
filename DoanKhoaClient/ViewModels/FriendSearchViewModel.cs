using DoanKhoaClient.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DoanKhoaClient.ViewModels
{
    public class FriendSearchViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private readonly User _currentUser;
        private string _searchQuery;
        private string _statusMessage;
        private bool _isSearching;
        private User _selectedUser;
        private ObservableCollection<User> _searchResults = new ObservableCollection<User>();

        public event EventHandler<bool> CloseDialogRequested;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    OnPropertyChanged();
                }
            }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsUserSelected));
                }
            }
        }

        public ObservableCollection<User> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }

        public bool IsUserSelected => SelectedUser != null;

        public ICommand SearchCommand { get; }
        public ICommand StartChatCommand { get; }

        public FriendSearchViewModel(User currentUser)
        {
            _currentUser = currentUser;
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5299/api/") };

            SearchCommand = new RelayCommand(_ => ExecuteSearch(), _ => !IsSearching);
            StartChatCommand = new RelayCommand(_ => StartChat(), _ => IsUserSelected);
        }

        private async void ExecuteSearch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
                {
                    StatusMessage = "Vui lòng nhập ít nhất 2 ký tự để tìm kiếm";
                    return;
                }

                IsSearching = true;
                StatusMessage = "Đang tìm kiếm...";
                SearchResults.Clear();

                var response = await _httpClient.GetAsync($"user/search?query={Uri.EscapeDataString(SearchQuery)}");

                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<User>>();

                    // Remove current user from results if present
                    users = users.Where(u => u.Id != _currentUser.Id).ToList();

                    foreach (var user in users)
                    {
                        SearchResults.Add(user);
                    }

                    if (SearchResults.Count == 0)
                    {
                        StatusMessage = "Không tìm thấy kết quả nào";
                    }
                    else
                    {
                        StatusMessage = $"Tìm thấy {SearchResults.Count} kết quả";
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    StatusMessage = $"Lỗi: {error}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void StartChat()
        {
            if (SelectedUser != null)
            {
                CloseDialogRequested?.Invoke(this, true);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Relay command implementation
        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter) => _execute(parameter);

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}