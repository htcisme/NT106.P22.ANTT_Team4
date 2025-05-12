using System;
using System.Globalization;
using System.Windows.Data;

namespace DoanKhoaClient.Converters
{
    public class TimeZoneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // Đảm bảo thời gian là UTC
                if (dateTime.Kind != DateTimeKind.Utc)
                {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }

                // Chuyển đổi sang giờ Việt Nam (UTC+7)
                TimeZoneInfo vietnamZone = TimeZoneInfo.CreateCustomTimeZone(
                    "Vietnam Standard Time",
                    new TimeSpan(7, 0, 0),
                    "Vietnam Standard Time",
                    "Vietnam Standard Time");

                DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, vietnamZone);

                // Format theo yêu cầu HH:mm
                if (parameter != null && parameter.ToString() == "HH:mm")
                {
                    return vietnamTime.ToString("HH:mm");
                }

                return vietnamTime;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}