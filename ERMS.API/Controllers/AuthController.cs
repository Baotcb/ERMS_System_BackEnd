using ERMS.Application.Features.Auth.Commands.ChangePassword;
using ERMS.Application.Features.Auth.Commands.ConfirmEmail;
using ERMS.Application.Features.Auth.Commands.ForgotPassword;
using ERMS.Application.Features.Auth.Commands.GoogleLogin;
using ERMS.Application.Features.Auth.Commands.Login;
using ERMS.Application.Features.Auth.Commands.Register;
using ERMS.Application.Features.Auth.Commands.ResendConfirmation;
using ERMS.Application.Features.Auth.Commands.ResetPassword;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("fixed")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly IMediator _mediator;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AuthController(ISender sender,
            IConfiguration config,
            ITokenService tokenService,
            IMediator mediator,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _sender = sender;
            _config = config;
            _tokenService = tokenService;
            _mediator = mediator;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            try
            {
                var userId = await _sender.Send(command);

                return Ok(new
                {
                    message = "Đăng ký thành công!",
                    userId = userId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            try
            {
                var token = await _sender.Send(command);
                return Ok(new
                {
                    token = token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            try
            {
                var token = await _sender.Send(command);

                return Ok(new { message = "Vui lòng kiểm tra email " });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var resultMessage = await _sender.Send(command);
                return Ok(new { message = resultMessage });
            }
            catch (Exception ex)
            {

                return BadRequest(new { message = ex.Message });
            }
        }

        
        [HttpPost("google-login")]
        [DisableRateLimiting]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.IdToken))
                {
                    return BadRequest(new { message = "Google ID token là bắt buộc." });
                }

                var token = await _sender.Send(command);
                return Ok(new
                {
                    token = token
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("google-login-redirect")]
        [DisableRateLimiting]
        public IActionResult GoogleLoginRedirect()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth", null, Request.Scheme);
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        [DisableRateLimiting]
        public async Task<IActionResult> GoogleResponse()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(
                    IdentityConstants.ExternalScheme);

                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Xác thực Google không thành công." });
                }

                
                var idToken = result.Properties?.GetTokenValue("id_token");
                
                
                if (string.IsNullOrWhiteSpace(idToken))
                {
                    idToken = result.Principal.FindFirstValue("id_token");
                }

                var email = result.Principal.FindFirstValue(ClaimTypes.Email);
                var name = result.Principal.FindFirstValue(ClaimTypes.Name);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Không thể lấy email từ Google." });
                }

                string jwtToken;
                
                if (!string.IsNullOrWhiteSpace(idToken))
                {
                    
                    jwtToken = await _mediator.Send(new GoogleLoginCommand
                    {
                        IdToken = idToken
                    });
                }
                else
                {
                    
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        // Tạo user mới từ claims
                        user = new User
                        {
                            UserName = email,
                            Email = email,
                            FullName = name ?? email.Split('@')[0],
                            EmailConfirmed = true,
                            DateJoined = DateTime.UtcNow
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            return BadRequest(new { message = $"Không thể tạo tài khoản: {errors}" });
                        }

                        // Gán role mặc định
                        if (!await _roleManager.RoleExistsAsync(AppRoles.Candidate))
                        {
                            await _roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Candidate));
                        }
                        await _userManager.AddToRoleAsync(user, AppRoles.Candidate);
                    }
                    else
                    {
                        // Cập nhật thông tin user nếu cần
                        if (!string.IsNullOrWhiteSpace(name) && user.FullName != name)
                        {
                            user.FullName = name;
                            user.UpdatedAt = DateTime.UtcNow;
                            await _userManager.UpdateAsync(user);
                        }
                    }

                    jwtToken = await _tokenService.CreateToken(user);
                }

                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                
                var frontendUrl = _config["ClientSettings:Url"] ?? "http://localhost:3000";
                return Redirect($"{frontendUrl}/auth/callback?token={jwtToken}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            try
            {
                var resultMessage = await _sender.Send(command);
                return Ok(new { message = resultMessage });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationCommand command)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command.Email))
                {
                    return BadRequest(new { message = "Email là bắt buộc." });
                }

                var result = await _mediator.Send(command);

                return Ok(new
                {
                    message = "Email xác thực đã được gửi. Vui lòng kiểm tra hộp thư (kể cả thư mục spam).",
                    email = command.Email
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau." });
            }
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            try
            {
                var message = await _mediator.Send(command);

                return Ok(new { message = message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xác thực email. Vui lòng thử lại sau." });
            }
        }
        [HttpGet("email-status")]
        [Authorize]
        public async Task<IActionResult> GetEmailStatus()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin người dùng." });
                }

                return Ok(new
                {
                    emailConfirmed = user.EmailConfirmed,
                    email = user.Email,
                    userId = user.Id,
                    fullName = user.FullName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy thông tin." });
            }
        }
        [HttpGet("confirm-email-redirect")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailRedirect([FromQuery] string userId, [FromQuery] string token)
        {
            try
            {
                var command = new ConfirmEmailCommand
                {
                    UserId = userId,
                    Token = token
                };

                var message = await _mediator.Send(command);

             
                var frontendUrl = _config["ClientSettings:Url"] ?? "http://localhost:3000";
                return Redirect($"{frontendUrl}/auth/email-confirmed?success=true&message={Uri.EscapeDataString(message)}");
            }
            catch (ArgumentException ex)
            {
                
                var frontendUrl = _config["ClientSettings:Url"] ?? "http://localhost:3000";
                return Redirect($"{frontendUrl}/auth/email-confirmed?success=false&message={Uri.EscapeDataString(ex.Message)}");
            }
            catch (Exception)
            {
                var frontendUrl = _config["ClientSettings:Url"] ?? "http://localhost:3000";
                return Redirect($"{frontendUrl}/auth/email-confirmed?success=false&message={Uri.EscapeDataString("Có lỗi xảy ra khi xác thực email.")}");
            }
        }


    } 
}

