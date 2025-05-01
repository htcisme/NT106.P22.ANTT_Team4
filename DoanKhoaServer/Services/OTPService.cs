using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace DoanKhoaServer.Services
{
    public class OtpService
    {
        private readonly ConcurrentDictionary<string, OtpInfo> _otpStore = new ConcurrentDictionary<string, OtpInfo>();
        private const int OTP_LENGTH = 6;
        private const int OTP_TTL_MINUTES = 10;

        public string GenerateOtp(string userId)
        {
            // Generate a random 6-digit OTP
            string otp = GenerateRandomOtp(OTP_LENGTH);

            // Store the OTP with expiration time
            _otpStore[userId] = new OtpInfo
            {
                Otp = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_TTL_MINUTES)
            };

            return otp;
        }

        public bool VerifyOtp(string userId, string otp)
        {
            // Check if the OTP exists for the user
            if (!_otpStore.TryGetValue(userId, out var otpInfo))
            {
                return false;
            }

            // Check if the OTP is valid and not expired
            bool isValid = otpInfo.Otp == otp && otpInfo.ExpiresAt > DateTime.UtcNow;

            // Remove the OTP if it's valid (one-time use)
            if (isValid)
            {
                _otpStore.TryRemove(userId, out _);
            }

            return isValid;
        }

        private string GenerateRandomOtp(int length)
        {
            const string chars = "0123456789";
            var random = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random);

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random[i] % chars.Length];
            }

            return new string(result);
        }

        private class OtpInfo
        {
            public string Otp { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}