using System;

namespace DoanKhoaClient.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.CreateCustomTimeZone("Vietnam Standard Time",
                                            new TimeSpan(7, 0, 0),
                                            "Vietnam Standard Time",
                                            "Vietnam Standard Time");

        /// <summary>
        /// Lấy thời gian hiện tại theo múi giờ Việt Nam (UTC+7)
        /// </summary>
        public static DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        /// <summary>
        /// Chuyển đổi từ thời gian UTC sang thời gian Việt Nam
        /// </summary>
        public static DateTime ConvertToVietnamTime(DateTime utcTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, VietnamTimeZone);
        }
    }
}