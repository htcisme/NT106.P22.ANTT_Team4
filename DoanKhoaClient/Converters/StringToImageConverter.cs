using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DoanKhoaClient.Converters
{
    public class StringToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imageUrl = value as string;
            System.Diagnostics.Debug.WriteLine($"[StringToImageConverter] Converting URL: {imageUrl}");

            if (string.IsNullOrEmpty(imageUrl))
            {
                System.Diagnostics.Debug.WriteLine("[StringToImageConverter] URL is null or empty");
                return null;
            }

            try
            {
                // Ensure URL is absolute
                if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                {
                    if (imageUrl.StartsWith("/Uploads/"))
                    {
                        imageUrl = $"http://localhost:5299{imageUrl}";
                    }
                    else if (!imageUrl.StartsWith("http"))
                    {
                        imageUrl = $"http://localhost:5299/Uploads/{Path.GetFileName(imageUrl)}";
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[StringToImageConverter] Final URL: {imageUrl}");

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();

                System.Diagnostics.Debug.WriteLine($"[StringToImageConverter] Successfully created bitmap for: {imageUrl}");
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StringToImageConverter] Error loading image: {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}