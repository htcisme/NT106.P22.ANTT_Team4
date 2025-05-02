using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.ViewModels;

namespace DoanKhoaClient.Views
{
    public partial class LightTasksView : Window
    {
        private readonly LightTasksViewModels _viewModel;

        public LightTasksView()
        {
            InitializeComponent();
            _viewModel = new LightTasksViewModels();
        }

        private void OnDarkModeClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.HandleDarkModeClick();
        }

        private void OnNotificationsClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.HandleNotificationsClick();
        }
    }
}