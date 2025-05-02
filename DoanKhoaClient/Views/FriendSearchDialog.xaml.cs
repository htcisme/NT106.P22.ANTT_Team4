using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.Views
{
    public partial class FriendSearchDialog : Window
    {
        private FriendSearchViewModel _viewModel;

        public User SelectedUser => _viewModel.SelectedUser;
        public bool DialogConfirmed { get; private set; } = false;

        public FriendSearchDialog(User currentUser)
        {
            InitializeComponent();
            _viewModel = new FriendSearchViewModel(currentUser);
            DataContext = _viewModel;

            // Subscribe to the dialog close event
            _viewModel.CloseDialogRequested += (s, confirmed) =>
            {
                DialogConfirmed = confirmed;
                DialogResult = confirmed;
            };
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.SearchCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}