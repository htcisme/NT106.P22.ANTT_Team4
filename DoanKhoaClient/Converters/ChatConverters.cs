using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DoanKhoaClient.Models;
using System.Windows.Media.Imaging;

namespace DoanKhoaClient.Converters
{
    public class CurrentUserConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Lấy ID của người dùng hiện tại - "1" là ID mẫu trong demo data
            string currentUserId = "1";

            bool isCurrentUser = value?.ToString() == currentUserId;
            return isCurrentUser ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotCurrentUserConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Lấy ID của người dùng hiện tại - "1" là ID mẫu trong demo data
            string currentUserId = "1";

            bool isCurrentUser = value?.ToString() == currentUserId;
            return isCurrentUser ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }




}