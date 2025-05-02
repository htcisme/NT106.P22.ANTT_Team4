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
            if (value is string senderId &&
                Application.Current.Properties.Contains("CurrentUser") &&
                Application.Current.Properties["CurrentUser"] is User currentUser)
            {
                // Debug line to help troubleshoot
                System.Diagnostics.Debug.WriteLine($"CurrentUserConverter: Message sender={senderId}, CurrentUser={currentUser.Id}, IsMatch={senderId == currentUser.Id}");

                return senderId == currentUser.Id ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
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
            if (value is string senderId &&
                Application.Current.Properties.Contains("CurrentUser") &&
                Application.Current.Properties["CurrentUser"] is User currentUser)
            {
                return senderId != currentUser.Id ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }




}