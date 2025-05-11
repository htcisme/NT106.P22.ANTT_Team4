using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;

namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskContentDesignView : Window
    {
        private TaskItemsViewModel _viewModel;

        public AdminTasksGroupTaskContentDesignView(TaskProgram program)
        {
            InitializeComponent();
            
            _viewModel = new TaskItemsViewModel(program);
            DataContext = _viewModel;

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_GroupTask_Design_Background);
        }
        
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Design_Background);
        }
    }
}