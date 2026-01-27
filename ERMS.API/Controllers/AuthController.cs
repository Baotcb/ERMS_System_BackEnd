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

        [HttpPost("confirm-email")]
        [DisableRateLimiting]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _mediator.Send(command);

                
                var user = await _userManager.FindByIdAsync(command.UserId);
                if (user != null)
                {
                    var token = await _tokenService.CreateToken(user);
                    return Ok(new
                    {
                        message = result,
                        success = true,
                        token = token,
                        email = user.Email,
                        fullName = user.FullName
                    });
                }

                return Ok(new
                {
                    message = result,
                    success = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    success = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Xác thực email thất bại. Vui lòng thử lại hoặc yêu cầu gửi lại email xác thực.",
                    error = ex.Message,
                    success = false
                });
            }
        }

        [HttpPost("resend-confirmation")]
        [DisableRateLimiting]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _sender.Send(command);
                

                return Ok(new
                {
                    message = "Email xác thực đã được gửi. Vui lòng kiểm tra hộp thư của bạn (bao gồm cả thư mục spam).",
                    success = result
            });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    success = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Không thể gửi email xác thực. Vui lòng thử lại sau.",
                    error = ex.Message,
                    success = false
                });
            }
        }




    } 
}

