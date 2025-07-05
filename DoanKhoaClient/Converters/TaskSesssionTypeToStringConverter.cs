using System;
using System.Globalization;
using System.Windows.Data;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Views
{
    public class TaskSessionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskSessionType type)
            {
                switch (type)
                {
                    case TaskSessionType.Event:
                        return "Sự kiện";
                    case TaskSessionType.Study:
                        return "Học tập";
                    case TaskSessionType.Design:
                        return "Thiết kế";
                    default:
                        return "Không xác định";
                }
            }
            return "Không xác định";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}