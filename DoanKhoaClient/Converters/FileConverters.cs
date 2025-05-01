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
            if (value is Attachment attachment && parameter is string type)
            {
                if (type.Equals("image", StringComparison.OrdinalIgnoreCase) && attachment.IsImage)
                    return Visibility.Visible;
                if (type.Equals("file", StringComparison.OrdinalIgnoreCase) && !attachment.IsImage)
                    return Visibility.Visible;

                return Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType && parameter is string type)
            {
                if (type.Equals("system", StringComparison.OrdinalIgnoreCase) && messageType == MessageType.System)
                    return Visibility.Visible;
                if (type.Equals("image", StringComparison.OrdinalIgnoreCase) && messageType == MessageType.Image)
                    return Visibility.Visible;
                if (type.Equals("file", StringComparison.OrdinalIgnoreCase) && messageType == MessageType.File)
                    return Visibility.Visible;
                if (type.Equals("text", StringComparison.OrdinalIgnoreCase) && messageType == MessageType.Text)
                    return Visibility.Visible;

                return Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string contentType = value as string;
            if (string.IsNullOrEmpty(contentType))
                return new BitmapImage(new Uri("/Views/Images/file_icon.png", UriKind.Relative));

            if (contentType.StartsWith("image/"))
                return new BitmapImage(new Uri("/Views/Images/image_icon.png", UriKind.Relative));
            else if (contentType.Contains("pdf"))
                return new BitmapImage(new Uri("/Views/Images/pdf_icon.png", UriKind.Relative));
            else if (contentType.Contains("word") || contentType.Contains("document"))
                return new BitmapImage(new Uri("/Views/Images/doc_icon.png", UriKind.Relative));
            else if (contentType.Contains("excel") || contentType.Contains("spreadsheet"))
                return new BitmapImage(new Uri("/Views/Images/xls_icon.png", UriKind.Relative));
            else if (contentType.Contains("zip") || contentType.Contains("compressed") || contentType.Contains("rar"))
                return new BitmapImage(new Uri("/Views/Images/zip_icon.png", UriKind.Relative));

            return new BitmapImage(new Uri("/Views/Images/file_icon.png", UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long size)
            {
                string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
                int i = 0;
                double dSize = size;

                while (dSize >= 1024 && i < suffixes.Length - 1)
                {
                    dSize /= 1024;
                    i++;
                }

                return $"{dSize:0.##} {suffixes[i]}";
            }

            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Thêm converter để xử lý string rỗng
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as string;
            return !string.IsNullOrEmpty(text);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}