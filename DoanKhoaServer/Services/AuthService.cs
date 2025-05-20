using DoanKhoaServer.Helpers;
using DoanKhoaServer.Models;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace DoanKhoaServer.Services
{
    public class AuthService
    {
        private readonly MongoDBService _mongoDBService;
        private readonly EmailService _emailService;

        public AuthService(MongoDBService mongoDBService, EmailService emailService)
        {
            _mongoDBService = mongoDBService;
            _emailService = emailService;
        }
        private string GenerateVerificationCode()
        {
            // Generate a 6-digit code
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        public async Task<(User user, string message)> RegisterUser(
    string username,
    string displayName,
    string email,
    string password,
    bool enableTwoFactorAuth,
    UserRole role = UserRole.User)
        {
            // Kiểm tra xem username đã tồn tại chưa
            var existingUser = await _mongoDBService.GetUserByUsernameAsync(username);
            if (existingUser != null)
            {
                return (null, "duplicate_username");
            }

            // Kiểm tra xem email đã được sử dụng chưa
            var existingEmail = await _mongoDBService.GetUserByEmailAsync(email);
            if (existingEmail != null)
            {
                return (null, "duplicate_email");
            }

            // Hash password
            var (hash, salt) = PasswordHelper.HashPassword(password);

            // Generate email verification code
            string verificationCode = GenerateVerificationCode();

            // Tạo user mới
            var newUser = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Username = username,
                DisplayName = displayName,
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                AvatarUrl = null,
                LastSeen = DateTime.UtcNow,
                TwoFactorEnabled = enableTwoFactorAuth,
                Role = role, // Role is already validated in the controller

                EmailVerified = false,
                EmailVerificationCode = verificationCode,
                EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15)

            };

            // Nếu bật xác thực 2 lớp, tạo secret key
            if (enableTwoFactorAuth)
            {
                newUser.TwoFactorSecret = TotpHelper.GenerateSecretKey();
            }

            // Send verification email
            try
            {
                await _emailService.SendVerificationEmailAsync(email, displayName, verificationCode);
            }
            catch (Exception ex)
            {
                return (null, $"email_error:{ex.Message}");
            }

            // Lưu vào database
            await _mongoDBService.CreateUserAsync(newUser);

            return (newUser, "registration_success_verification_required");
        }

        public async Task<(User user, string message)> VerifyEmail(string userId, string code)
        {
            var user = await _mongoDBService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return (null, "Tài khoản không tồn tại.");
            }

            if (user.EmailVerified)
            {
                return (user, "Email đã được xác thực trước đó.");
            }

            if (user.EmailVerificationCodeExpiry < DateTime.UtcNow)
            {
                return (null, "Mã xác thực đã hết hạn.");
            }

            if (user.EmailVerificationCode != code)
            {
                return (null, "Mã xác thực không chính xác.");
            }

            // Update user as verified
            user.EmailVerified = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiry = null;

            await _mongoDBService.UpdateUserAsync(user);

            return (user, "Email đã được xác thực thành công.");
        }
        public async Task<(User user, bool requiresTwoFactor, string message)> LoginUser(string username, string password)
        {
            // Tìm user theo username
            var user = await _mongoDBService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return (null, false, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            // Kiểm tra mật khẩu
            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return (null, false, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            // Kiểm tra xem cần xác thực 2 lớp không
            if (user.TwoFactorEnabled)
            {
                // Tạo mã OTP và lưu vào user
                string otp = TotpHelper.GenerateOtp();
                user.CurrentTwoFactorCode = otp;
                user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(3); // OTP có hiệu lực 3 phút

                // Cập nhật thông tin vào database
                await _mongoDBService.UpdateUserAsync(user);

                // Trong trường hợp thật sẽ gửi OTP qua email, nhưng ở đây chỉ để hiển thị
                return (user, true, $"Mã xác thực đã được gửi đến email của bạn. Mã: {otp}");
            }

            // Cập nhật LastSeen
            user.LastSeen = DateTime.UtcNow;
            await _mongoDBService.UpdateUserAsync(user);

            return (user, false, "Đăng nhập thành công");
        }

        public async Task<(User user, string message)> VerifyOtp(string userId, string otp)
        {
            var user = await _mongoDBService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return (null, "Người dùng không tồn tại.");
            }

            // Kiểm tra OTP
            if (!TotpHelper.ValidateOtp(otp, user.CurrentTwoFactorCode, user.TwoFactorCodeExpiry))
            {
                return (null, "Mã xác thực không đúng hoặc đã hết hạn.");
            }

            // Xác thực thành công, xóa mã OTP
            user.CurrentTwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;
            user.LastSeen = DateTime.UtcNow;

            // Cập nhật thông tin vào database
            await _mongoDBService.UpdateUserAsync(user);

            return (user, "Xác thực thành công");
        }
    }
}