using DoanKhoaServer.DTOs;
using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
public class VerifyEmailRequest
{
    public string UserId { get; set; }
    public string Code { get; set; }
}

public class ResetPasswordRequest
{
    public string UserId { get; set; }
    public string NewPassword { get; set; }
}

namespace DoanKhoaServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly MongoDBService _mongoDBService;
        public class PromoteToAdminRequest
        {
            public User User { get; set; }
            public string AdminCode { get; set; }
        }
        public UserController(AuthService authService, MongoDBService mongoDBService)
        {
            _authService = authService;
            _mongoDBService = mongoDBService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) ||
                    string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.DisplayName))
                {
                    return BadRequest("validation_error:Vui lòng nhập đầy đủ thông tin.");
                }

                // Admin validation
                if (request.Role == UserRole.Admin)
                {
                    // Define your secret admin code - in a real app, this should be in a secure config
                    const string ADMIN_SECRET_CODE = "DoanKhoaMMT";

                    if (string.IsNullOrEmpty(request.AdminCode) || request.AdminCode != ADMIN_SECRET_CODE)
                    {
                        return BadRequest("admin_code_error:Mã xác thực Admin không hợp lệ.");
                    }
                }

                // Register user with validated role
                var (user, message) = await _authService.RegisterUser(
                    request.Username,
                    request.DisplayName,
                    request.Email,
                    request.Password,
                    request.EnableTwoFactorAuth,
                    request.Role
                );

                if (user == null)
                {
                    // Return specific error message
                    return BadRequest(message);
                }

                // Registration successful, but email verification required
                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role,
                    Message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản.",
                    RequiresEmailVerification = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"server_error:{ex.Message}");
            }
        }

        // Add this action method
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest("Vui lòng nhập đầy đủ thông tin.");
                }

                var (user, message) = await _authService.VerifyEmail(request.UserId, request.Code);

                if (user == null)
                {
                    return BadRequest(message);
                }

                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Vui lòng nhập đầy đủ thông tin đăng nhập.");
                }

                // Đăng nhập
                var (user, requiresTwoFactor, message) = await _authService.LoginUser(
                    request.Username,
                    request.Password
                );

                if (user == null)
                {
                    return BadRequest(message);
                }

                // Trả về kết quả
                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    RequiresTwoFactor = requiresTwoFactor,
                    Message = message,
                    Role = user.Role // Include role in response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
        {
            try
            {
                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Otp))
                {
                    return BadRequest("Vui lòng nhập đầy đủ thông tin.");
                }

                // Xác thực OTP
                var (user, message) = await _authService.VerifyOtp(
                    request.UserId,
                    request.Otp
                );

                if (user == null)
                {
                    return BadRequest(message);
                }

                // Trả về kết quả
                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest("Từ khóa tìm kiếm phải có ít nhất 2 ký tự.");
                }

                // Lấy danh sách người dùng từ MongoDB service
                var users = await _mongoDBService.SearchUsersAsync(query);

                // Loại bỏ thông tin nhạy cảm trước khi trả về
                var userDtos = users.Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.DisplayName,
                    u.Email,
                    u.AvatarUrl,
                    u.LastSeen,
                    u.Position,
                    u.ActivitiesCount
                }).ToList();

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser([FromHeader(Name = "Authorization")] string authHeader)
        {
            try
            {
                // Extract user ID from token (simplified for demo)
                // In a real application, you would use proper JWT token validation
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized("Invalid authentication token");
                }

                string token = authHeader.Substring("Bearer ".Length);
                string userId = token; // In real app, decode JWT token to get user ID

                var user = await _mongoDBService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Return user without sensitive info
                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.DisplayName,
                    user.Email,
                    user.AvatarUrl,
                    user.LastSeen,
                    user.Conversations,
                    user.Role,
                    user.Position,
                    user.ActivitiesCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var user = await _mongoDBService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Return user without sensitive info
                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.DisplayName,
                    user.Email,
                    user.AvatarUrl,
                    user.LastSeen,
                    user.ActivitiesCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _mongoDBService.GetAllUsersAsync();

                // Return users without sensitive information
                var usersDto = users.Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.DisplayName,
                    u.Email,
                    u.AvatarUrl,
                    u.LastSeen,
                    u.Role,
                    u.Position,
                    u.ActivitiesCount
                }).ToList();

                return Ok(usersDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest("Mật khẩu mới không được để trống.");
                }

                if (request.NewPassword.Length < 6)
                {
                    return BadRequest("Mật khẩu phải có ít nhất 6 ký tự.");
                }

                // Kiểm tra user tồn tại
                var user = await _mongoDBService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Không tìm thấy người dùng.");
                }

                // Tạo mật khẩu mới
                var (hashedPassword, salt) = _authService.CreatePasswordHash(request.NewPassword);

                // Cập nhật mật khẩu
                user.PasswordHash = hashedPassword;
                user.PasswordSalt = salt;

                await _mongoDBService.UpdateUserAsync(user);

                return Ok(new { message = "Đặt lại mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
        [HttpPut("batch-update")]
        public async Task<IActionResult> BatchUpdateUsers([FromBody] BatchUpdateUserRequest request)
        {
            try
            {
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest("Danh sách người dùng không được để trống.");
                }

                if (request.Updates == null)
                {
                    return BadRequest("Dữ liệu cập nhật không được để trống.");
                }

                var results = new List<object>();
                var errors = new List<string>();

                foreach (var userId in request.UserIds)
                {
                    try
                    {
                        var user = await _mongoDBService.GetUserByIdAsync(userId);
                        if (user == null)
                        {
                            errors.Add($"Không tìm thấy người dùng với ID: {userId}");
                            continue;
                        }

                        // Kiểm tra Admin Code nếu đang nâng cấp lên Admin
                        if (request.Updates.Role == UserRole.Admin && user.Role != UserRole.Admin)
                        {
                            const string ADMIN_SECRET_CODE = "DoanKhoaMMT";
                            if (string.IsNullOrEmpty(request.AdminCode) || request.AdminCode != ADMIN_SECRET_CODE)
                            {
                                errors.Add($"Mã xác thực Admin không hợp lệ cho người dùng: {user.DisplayName}");
                                continue;
                            }
                        }

                        // Cập nhật các trường
                        if (request.Updates.Role.HasValue)
                        {
                            user.Role = request.Updates.Role.Value;
                        }

                        if (request.Updates.Position.HasValue)
                        {
                            user.Position = request.Updates.Position.Value;
                        }

                        if (request.Updates.EmailVerified.HasValue)
                        {
                            user.EmailVerified = request.Updates.EmailVerified.Value;
                        }

                        // Lưu thay đổi
                        await _mongoDBService.UpdateUserAsync(user);

                        results.Add(new
                        {
                            userId = user.Id,
                            username = user.Username,
                            displayName = user.DisplayName,
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi cập nhật người dùng {userId}: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    success = results.Count,
                    total = request.UserIds.Count,
                    errors = errors,
                    updatedUsers = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto updatedUser)
        {
            try
            {
                // Kiểm tra xem user có tồn tại không
                var user = await _mongoDBService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Không tìm thấy người dùng.");
                }

                // Validation cơ bản
                if (string.IsNullOrWhiteSpace(updatedUser.DisplayName))
                {
                    return BadRequest("Họ tên không được để trống.");
                }

                if (string.IsNullOrWhiteSpace(updatedUser.Email))
                {
                    return BadRequest("Email không được để trống.");
                }

                // Kiểm tra email format
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                if (!emailRegex.IsMatch(updatedUser.Email))
                {
                    return BadRequest("Email không hợp lệ.");
                }

                // Kiểm tra email đã tồn tại chưa (trừ email của chính user này)
                var existingUserWithEmail = await _mongoDBService.GetUserByEmailAsync(updatedUser.Email);
                if (existingUserWithEmail != null && existingUserWithEmail.Id != id)
                {
                    return BadRequest("Email này đã được sử dụng bởi người dùng khác.");
                }

                // Cập nhật các trường cho phép chỉnh sửa
                user.DisplayName = updatedUser.DisplayName.Trim();
                user.Email = updatedUser.Email.Trim();
                user.Role = updatedUser.Role;
                user.Position = updatedUser.Position;

                // Các trường khác có thể cập nhật
                if (!string.IsNullOrEmpty(updatedUser.AvatarUrl))
                {
                    user.AvatarUrl = updatedUser.AvatarUrl;
                }

                // Lưu thay đổi
                await _mongoDBService.UpdateUserAsync(user);

                // Trả về user đã được cập nhật (loại bỏ thông tin nhạy cảm)
                var result = new
                {
                    user.Id,
                    user.Username,
                    user.DisplayName,
                    user.Email,
                    user.AvatarUrl,
                    user.LastSeen,
                    user.Role,
                    user.Position,
                    user.ActivitiesCount,
                    user.EmailVerified,
                    user.TwoFactorEnabled
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _mongoDBService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Không tìm thấy người dùng");
                }

                await _mongoDBService.DeleteUserAsync(id);

                return Ok(new { message = "Người dùng đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut("{id}/promote-to-admin")]
        public async Task<IActionResult> PromoteToAdmin(string id, [FromBody] PromoteToAdminRequest request)
        {
            try
            {
                // Validate admin code
                const string ADMIN_SECRET_CODE = "DoanKhoaMMT"; // Nên đặt trong config
                if (string.IsNullOrEmpty(request.AdminCode) || request.AdminCode != ADMIN_SECRET_CODE)
                {
                    return BadRequest("Mã xác thực Admin không hợp lệ.");
                }

                var user = await _mongoDBService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Không tìm thấy người dùng");
                }

                // Cập nhật người dùng thành Admin
                user.Role = UserRole.Admin;
                // Cập nhật các trường khác từ request.User
                user.DisplayName = request.User.DisplayName;
                user.Email = request.User.Email;
                user.Position = request.User.Position;

                await _mongoDBService.UpdateUserAsync(user);

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
    }
}