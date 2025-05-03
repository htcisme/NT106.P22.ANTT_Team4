using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DoanKhoaClient.Helpers
{
    public class ThemeManager
    {
        public static bool IsDarkMode { get; private set; } = false;

        // Image paths
        private static readonly string LightThemeIcon = "/Views/Images/dark.png";
        private static readonly string DarkThemeIcon = "/Views/Images/light.png";
        private static readonly string LightNotificationsIcon = "/Views/Images/light-notifications.png";
        private static readonly string DarkNotificationsIcon = "/Views/Images/dark-notifications.png";

        public static void ToggleTheme(FrameworkElement root)
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme(root);
        }

        public static void ApplyTheme(FrameworkElement root)
        {
            // Get theme resources
            var lightBackground = (LinearGradientBrush)Application.Current.Resources["LightBackground"];
            var darkBackground = (LinearGradientBrush)Application.Current.Resources["DarkBackground"];
            var lightTextColor = (SolidColorBrush)Application.Current.Resources["LightTextColor"];
            var darkTextColor = (SolidColorBrush)Application.Current.Resources["DarkTextColor"];

            // Apply to grid background
            var grid = root as Grid;
            if (grid != null)
            {
                grid.Background = IsDarkMode ? darkBackground : lightBackground;
            }

            // Apply to theme button
            var themeButton = FindElementByName(root, "ThemeToggleButton") as Image;
            if (themeButton != null)
            {
                themeButton.Source = new BitmapImage(new Uri(
                    IsDarkMode ? DarkThemeIcon : LightThemeIcon,
                    UriKind.Relative));
            }

            // Apply to notifications
            var notificationsIcon = FindElementByName(root, "Task_iNotifications") as Image;
            if (notificationsIcon != null)
            {
                notificationsIcon.Source = new BitmapImage(new Uri(
                    IsDarkMode ? DarkNotificationsIcon : LightNotificationsIcon,
                    UriKind.Relative));
            }

            // Apply to all labels
            var labels = FindElementsByType<Label>(root);
            foreach (var label in labels)
            {
                // Skip labels that should maintain their color
                if (label.Name.EndsWith("lbTasks") &&
                    label.Foreground is SolidColorBrush brush &&
                    brush.Color.ToString() == "#FF597CA2")
                    continue;

                label.Foreground = IsDarkMode ? darkTextColor : lightTextColor;
            }

            // Apply to rectangle
            var divider = FindElementByName(root, "DividerRectangle") as System.Windows.Shapes.Rectangle;
            if (divider != null)
            {
                divider.Stroke = IsDarkMode ? darkTextColor : lightTextColor;
            }
        }

        private static FrameworkElement FindElementByName(FrameworkElement root, string name)
        {
            return root.FindName(name) as FrameworkElement;
        }

        private static System.Collections.Generic.List<T> FindElementsByType<T>(DependencyObject root) where T : DependencyObject
        {
            var results = new System.Collections.Generic.List<T>();
            var count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is T element)
                    results.Add(element);

                results.AddRange(FindElementsByType<T>(child));
            }

            return results;
        }
    }
}