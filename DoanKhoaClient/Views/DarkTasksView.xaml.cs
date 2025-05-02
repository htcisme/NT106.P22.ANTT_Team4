using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.ViewModels;

namespace DoanKhoaClient.Views
{
    public partial class DarkTasksView : Window
    {
        private readonly DarkTasksViewModels _viewModel;

        public DarkTasksView()
        {
            InitializeComponent();
            _viewModel = new DarkTasksViewModels();
        }

        private void OnLightModeClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.HandleLightModeClick();
        }

        private void OnNotificationsClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.HandleNotificationsClick();
        }
    }
}