using System;
using System.Collections.Generic;
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

        // Original text color cache to restore when needed - use FrameworkElement as the key type
        private static readonly Dictionary<FrameworkElement, Brush> OriginalTextColors = new Dictionary<FrameworkElement, Brush>();

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

            // Apply to all controls with text
            ApplyThemeToLabels(root);
            ApplyThemeToTextBlocks(root);
            ApplyThemeToButtons(root);

            // Apply to rectangle
            var divider = FindElementByName(root, "DividerRectangle") as System.Windows.Shapes.Rectangle;
            if (divider != null)
            {
                divider.Stroke = IsDarkMode ? darkTextColor : lightTextColor;
            }
        }

        private static void ApplyThemeToLabels(DependencyObject root)
        {
            var labels = FindElementsByType<Label>(root);
            foreach (var label in labels)
            {
                ApplyControlTextColor(label, label.Background);
            }
        }

        private static void ApplyThemeToTextBlocks(DependencyObject root)
        {
            var textBlocks = FindElementsByType<TextBlock>(root);
            foreach (var textBlock in textBlocks)
            {
                // Get parent background
                Brush parentBackground = GetParentBackground(textBlock);
                ApplyTextBlockTextColor(textBlock, parentBackground);
            }
        }

        private static void ApplyThemeToButtons(DependencyObject root)
        {
            var buttons = FindElementsByType<Button>(root);
            foreach (var button in buttons)
            {
                ApplyControlTextColor(button, button.Background);
            }
        }

        // New method to get parent background
        private static Brush GetParentBackground(DependencyObject element)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is Control control && control.Background != null)
                {
                    return control.Background;
                }
                else if (parent is Panel panel && panel.Background != null)
                {
                    return panel.Background;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private static void ApplyControlTextColor(Control control, Brush background)
        {
            // Don't process controls that should maintain their color regardless
            if (control is Label label && label.Name != null && label.Name.EndsWith("lbTasks") && 
                label.Foreground is SolidColorBrush brush && 
                brush.Color.ToString() == "#FF597CA2")
                return;
            
            // Store original color if not already saved
            if (!OriginalTextColors.ContainsKey(control))
            {
                OriginalTextColors[control] = control.Foreground?.Clone();
            }

            var darkTextColor = (SolidColorBrush)Application.Current.Resources["DarkTextColor"];
            var lightTextColor = (SolidColorBrush)Application.Current.Resources["LightTextColor"];

            // Get background color
            bool isOnDarkBackground = IsOnDarkBackground(control);

            if (isOnDarkBackground)
            {
                // Element is on a dark background, use white text
                control.Foreground = lightTextColor;
            }
            else if (IsDarkMode)
            {
                // In dark mode, but not on dark background - use light text
                control.Foreground = lightTextColor;
            }
            else
            {
                // In light mode, not on dark background - use dark text
                control.Foreground = darkTextColor;
                
                // Some special cases might need original color
                if (OriginalTextColors[control] is SolidColorBrush originalBrush &&
                    !IsWhiteBrush(originalBrush) && !IsBlackBrush(originalBrush))
                {
                    control.Foreground = OriginalTextColors[control];
                }
            }
        }

        private static void ApplyTextBlockTextColor(TextBlock textBlock, Brush background)
        {
            // Store original color if not already saved
            if (!OriginalTextColors.ContainsKey(textBlock))
            {
                OriginalTextColors[textBlock] = textBlock.Foreground?.Clone();
            }

            var darkTextColor = (SolidColorBrush)Application.Current.Resources["DarkTextColor"];
            var lightTextColor = (SolidColorBrush)Application.Current.Resources["LightTextColor"];

            // Special handling for chat bubble text
            if (IsChatBubbleText(textBlock))
            {
                // Always use dark text for chat bubbles regardless of theme
                textBlock.Foreground = darkTextColor;
                return;
            }

            // Get background color
            bool isOnDarkBackground = IsOnDarkBackground(textBlock);

            if (isOnDarkBackground)
            {
                // Element is on a dark background, use white text
                textBlock.Foreground = lightTextColor;
            }
            else if (IsDarkMode)
            {
                // In dark mode, but not on dark background - use light text
                textBlock.Foreground = lightTextColor;
            }
            else
            {
                // In light mode, not on dark background - use dark text
                textBlock.Foreground = darkTextColor;

                // Some special cases might need original color
                if (OriginalTextColors[textBlock] is SolidColorBrush originalBrush &&
                    !IsWhiteBrush(originalBrush) && !IsBlackBrush(originalBrush))
                {
                    textBlock.Foreground = OriginalTextColors[textBlock];
                }
            }
        }

        // New helper method to detect chat bubble text
        // New helper method to detect chat bubble text
        private static bool IsChatBubbleText(TextBlock textBlock)
        {
            // Check if the textblock is inside a Border with a specific background color
            DependencyObject parent = VisualTreeHelper.GetParent(textBlock);
            while (parent != null)
            {
                if (parent is Border border && border.Background != null)
                {
                    if (border.Background is SolidColorBrush brush)
                    {
                        // Check for chat bubble backgrounds
                        string colorStr = brush.Color.ToString();
                        if (colorStr == "#FFB9D4EB" || colorStr == "#FFD5E0E9" ||
                            colorStr == "#FFF0F0F0" || colorStr == "#FFE8E8E8" ||  // Added system message color
                            colorStr.StartsWith("#FFE"))
                        {
                            return true;
                        }
                    }
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return false;
        }

        // New helper methods
        private static bool IsWhiteBrush(SolidColorBrush brush)
        {
            return brush.Color.R > 240 && brush.Color.G > 240 && brush.Color.B > 240;
        }

        private static bool IsBlackBrush(SolidColorBrush brush)
        {
            return brush.Color.R < 30 && brush.Color.G < 30 && brush.Color.B < 30;
        }

        private static bool IsOnDarkBackground(DependencyObject element)
        {
            // Check if element or any of its parents has a dark background
            DependencyObject current = element;
            
            while (current != null)
            {
                if (current is Control control && control.Background != null)
                {
                    if (IsColoredOrDarkBackground(control.Background))
                        return true;
                }
                else if (current is Panel panel && panel.Background != null)
                {
                    if (IsColoredOrDarkBackground(panel.Background))
                        return true;
                }
                
                current = VisualTreeHelper.GetParent(current);
            }
            
            return false;
        }

        private static bool IsColoredOrDarkBackground(Brush background)
        {
            if (background is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                
                // Check if color is dark
                if (color.A > 0)
                {
                    // Calculate luminance (brightness perception formula)
                    double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                    
                    // If luminance < 0.5, it's a dark color
                    if (luminance < 0.5)
                        return true;
                        
                    // Also consider certain colors as "colored" backgrounds
                    if (!(color.R > 240 && color.G > 240 && color.B > 240))
                    {
                        // Check for significant color presence (non-grayscale)
                        int max = Math.Max(Math.Max(color.R, color.G), color.B);
                        int min = Math.Min(Math.Min(color.R, color.G), color.B);
                        if (max - min > 30) // Color saturation threshold
                            return true;
                    }
                }
                return false;
            }
            else if (background is LinearGradientBrush gradientBrush)
            {
                // Check the gradient stops for dark colors
                foreach (var stop in gradientBrush.GradientStops)
                {
                    // Calculate luminance
                    double luminance = (0.299 * stop.Color.R + 0.587 * stop.Color.G + 0.114 * stop.Color.B) / 255;
                    if (luminance < 0.5)
                        return true;
                }
                return false;
            }
            
            return false;
        }

        private static FrameworkElement FindElementByName(FrameworkElement root, string name)
        {
            return root.FindName(name) as FrameworkElement;
        }

        private static List<T> FindElementsByType<T>(DependencyObject root) where T : DependencyObject
        {
            var results = new List<T>();
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