using System;
using System.Globalization;
using System.Windows.Data;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Converters
{
    public class RoleToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserRole role)
            {
                return role switch
                {
                    UserRole.Admin => "Quản trị",
                    UserRole.User => "Người dùng",
                    _ => role.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 