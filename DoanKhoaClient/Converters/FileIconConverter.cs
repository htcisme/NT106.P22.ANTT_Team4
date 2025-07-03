using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DoanKhoaClient.Converters
{
    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string contentType)
            {
                if (contentType.StartsWith("image/"))
                {
                    return new BitmapImage(new Uri("/Views/Images/image_file.png", UriKind.Relative));
                }
                else if (contentType.Contains("pdf"))
                {
                    return new BitmapImage(new Uri("/Views/Images/pdf_file.png", UriKind.Relative));
                }
                else if (contentType.Contains("word") || contentType.Contains("document"))
                {
                    return new BitmapImage(new Uri("/Views/Images/doc_file.png", UriKind.Relative));
                }
                else if (contentType.Contains("excel") || contentType.Contains("sheet"))
                {
                    return new BitmapImage(new Uri("/Views/Images/xls_file.png", UriKind.Relative));
                }
                else if (contentType.Contains("zip") || contentType.Contains("rar") || contentType.Contains("compressed"))
                {
                    return new BitmapImage(new Uri("/Views/Images/zip_file.png", UriKind.Relative));
                }
                else if (contentType.Contains("text"))
                {
                    return new BitmapImage(new Uri("/Views/Images/txt_file.png", UriKind.Relative));
                }
                else
                {
                    return new BitmapImage(new Uri("/Views/Images/generic_file.png", UriKind.Relative));
                }
            }

            return new BitmapImage(new Uri("/Views/Images/generic_file.png", UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}