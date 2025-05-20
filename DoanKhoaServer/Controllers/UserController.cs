using DoanKhoaServer.DTOs;
using DoanKhoaServer.Models;
using DoanKhoaServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
                    return BadRequest("Vui lòng nhập đầy đủ thông tin.");
                }

                // Admin validation
                if (request.Role == UserRole.Admin)
                {
                    // Define your secret admin code - in a real app, this should be in a secure config
                    const string ADMIN_SECRET_CODE = "DoanKhoaMMT";

                    if (string.IsNullOrEmpty(request.AdminCode) || request.AdminCode != ADMIN_SECRET_CODE)
                    {
                        return BadRequest("Mã xác thực Admin không hợp lệ.");
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
                    return BadRequest(message);
                }

                // Trả về kết quả thành công
                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role, // Include role in response
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User updatedUser)
        {
            try
            {
                var user = await _mongoDBService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound("User not found");
                }
                // Cập nhật các trường cho phép chỉnh sửa
                user.DisplayName = updatedUser.DisplayName;
                user.Email = updatedUser.Email;
                user.Role = updatedUser.Role;
                user.Position = updatedUser.Position;

                await _mongoDBService.UpdateUserAsync(user);

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
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