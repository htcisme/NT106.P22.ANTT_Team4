using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DoanKhoaClient.Converters
{
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter != null && parameter.ToString() == "inverse";
            bool isNullOrEmpty = string.IsNullOrEmpty(value as string);

            if (inverse)
                return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
            else
                return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}