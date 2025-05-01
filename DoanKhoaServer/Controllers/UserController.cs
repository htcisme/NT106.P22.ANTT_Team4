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

        public UserController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) ||
                    string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.DisplayName))
                {
                    return BadRequest("Vui lòng nhập đầy đủ thông tin.");
                }

                // Đăng ký người dùng
                var (user, message) = await _authService.RegisterUser(
                    request.Username,
                    request.DisplayName,
                    request.Email,
                    request.Password,
                    request.EnableTwoFactorAuth
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
                    Message = message
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
    }
}