using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DoanKhoaClient.Helpers;
using System.Windows;
using System.ComponentModel;

namespace DoanKhoaClient.Views
{
    public partial class RegisterView : Window
    {
        public RegisterView()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(Register_Background);
        }

        private void ThemeToggleButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.ToggleTheme(Register_Background);
        }

        // Override the window closing event to navigate back to login
        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                // Check if there's already a login window open
                bool hasLoginWindow = false;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginView && window != this)
                    {
                        hasLoginWindow = true;
                        window.Activate(); // Bring the login window to front
                        break;
                    }
                }

                // If no login window exists, create one
                if (!hasLoginWindow)
                {
                    var loginWindow = new LoginView();
                    loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    loginWindow.Show();

                    // Set the new login window as the main window
                    Application.Current.MainWindow = loginWindow;
                }

                base.OnClosing(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during window closing: {ex.Message}");
                // Still allow the window to close even if there's an error
                base.OnClosing(e);
            }
        }
    }
}