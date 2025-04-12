using DoanKhoaClient.Helpers;
using System.Windows.Input;
using System.Windows;

namespace DoanKhoaClient.ViewModels
{
    public class DashboardViewModel
    {
        public ICommand ShowDetailCommand { get; }

        public DashboardViewModel()
        {
            ShowDetailCommand = new RelayCommand(OnShowDetail);
        }

        private void OnShowDetail(object obj)
        {
            MessageBox.Show("Bạn vừa nhấn double click!");
        }
    }
}
