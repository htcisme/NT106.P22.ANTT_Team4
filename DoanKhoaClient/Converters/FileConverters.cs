using DoanKhoaClient.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DoanKhoaClient.Converters
{
    public class AttachmentTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType && parameter is string expectedType)
            {
                if ((messageType == MessageType.Image && expectedType.Equals("Image", StringComparison.OrdinalIgnoreCase)) ||
                    (messageType == MessageType.File && expectedType.Equals("File", StringComparison.OrdinalIgnoreCase)))
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Tách thành class riêng, không còn là class lồng nhau
    public class MessageTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType && parameter is string expectedType)
            {
                if ((messageType == MessageType.Text && expectedType.Equals("Text", StringComparison.OrdinalIgnoreCase)))
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return !string.IsNullOrEmpty(text);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}