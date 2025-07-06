using DoanKhoaClient.Models;
using DoanKhoaClient.ViewModels;
using DoanKhoaClient.Helpers;
using System.Windows;
using System.Windows.Input;
using DoanKhoaClient.Services; // ✅ THÊM: TaskService
using System.Windows.Controls; // ✅ THÊM: CheckBox
namespace DoanKhoaClient.Views
{
    public partial class AdminTasksGroupTaskContentStudyView : Window
    {
        private TaskItemsViewModel _viewModel;

        public AdminTasksGroupTaskContentStudyView(TaskProgram program)
        {
            InitializeComponent();

            _viewModel = new TaskItemsViewModel(program);
            DataContext = _viewModel;

            // Áp dụng theme
            ThemeManager.ApplyTheme(Admin_GroupTask_Study_Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void TaskItem_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskItem taskItem)
            {
                if (!_viewModel.SelectedTaskItems.Contains(taskItem))
                {
                    _viewModel.SelectedTaskItems.Add(taskItem);
                }
            }
        }
        private void TaskItem_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskItem taskItem)
            {
                _viewModel.SelectedTaskItems.Remove(taskItem);
            }
        }
        private void ThemeToggleButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Admin_GroupTask_Study_Background);
        }
    }
}