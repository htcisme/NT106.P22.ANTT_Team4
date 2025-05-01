using System;
using System.Security.Cryptography;
using System.Text;

namespace DoanKhoaServer.Helpers
{
    public static class TotpHelper
    {
        private const int CodeLength = 6;
        private const int ValidityPeriod = 180; // 3 phút

        // Tạo mã OTP ngẫu nhiên dạng số
        public static string GenerateOtp()
        {
            Random random = new Random();
            string otp = "";

            for (int i = 0; i < CodeLength; i++)
            {
                otp += random.Next(0, 10).ToString();
            }

            return otp;
        }

        // Tạo secret key cho người dùng khi đăng ký 2FA
        public static string GenerateSecretKey()
        {
            var key = new byte[20]; // 160-bit key
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }

            return Convert.ToBase64String(key);
        }

        // Kiểm tra xem mã OTP có hợp lệ không
        public static bool ValidateOtp(string inputOtp, string expectedOtp, DateTime? codeExpiry)
        {
            // Kiểm tra xem mã OTP có đúng không
            if (string.IsNullOrEmpty(inputOtp) || inputOtp != expectedOtp)
                return false;

            // Kiểm tra xem mã OTP có còn hiệu lực không
            if (!codeExpiry.HasValue || DateTime.UtcNow > codeExpiry.Value)
                return false;

            return true;
        }
    }
}