using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Services;

namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskContentEventView : Window
    {
        private readonly TaskService _taskService;
        private TaskItemsViewModel _viewModel;


        public AdminTasksGroupTaskContentEventView(TaskProgram program)
        {
            InitializeComponent();
            _taskService = new TaskService();
            _viewModel = new TaskItemsViewModel(program, _taskService);
            DataContext = _viewModel;

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_GroupTask_Event_Background);

            // Ensure DataContext is set after InitializeComponent
            this.Loaded += (sender, e) =>
            {
                // Refresh data when window is loaded
                _viewModel.RefreshCommand.Execute(null);
            };
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Event_Background);
        }
    }
}