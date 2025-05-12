using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DoanKhoaClient.Helpers;

using System.Windows;
namespace DoanKhoaClient.Views

{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Login_Background);
            this.SizeChanged += (sender, e) =>
            {
                if (this.ActualWidth < this.MinWidth || this.ActualHeight < this.MinHeight)
                {
                    this.WindowState = WindowState.Normal;
                }
            };
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Login_Background);
        }
    }
}
