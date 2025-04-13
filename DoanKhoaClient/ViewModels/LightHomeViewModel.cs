using DoanKhoaClient.Helpers;
using System.Windows.Input;
using System.Windows;

namespace DoanKhoaClient.ViewModels
{
    public class LightHomeViewModel
    {
        public ICommand ShowDetailCommand { get; }

        public LightHomeViewModel()
        {
            ShowDetailCommand = new RelayCommand(OnShowDetail);
        }

        private void OnShowDetail(object obj)
        {
            MessageBox.Show("Bạn vừa nhấn double click!");
        }
    }
}
