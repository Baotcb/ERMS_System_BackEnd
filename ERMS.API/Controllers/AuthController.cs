using ERMS.Application.Features.Auth.Commands.ChangePassword;
using ERMS.Application.Features.Auth.Commands.ConfirmEmail;
using ERMS.Application.Features.Auth.Commands.ForgotPassword;
using ERMS.Application.Features.Auth.Commands.GoogleLogin;
using ERMS.Application.Features.Auth.Commands.Login;
using ERMS.Application.Features.Auth.Commands.Register;
using ERMS.Application.Features.Auth.Commands.ResendConfirmation;
using ERMS.Application.Features.Auth.Commands.ResetPassword;
using ERMS.Application.Features.Auth.Commands.CreateHRAccount;
using ERMS.Application.Features.Enterprises.Commands.RegisterEnterprise;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace ERMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("fixed")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IMediator _mediator;

        public AuthController(ISender sender, IMediator mediator)
        {
            _sender = sender;
            _mediator = mediator;
        }

        // ================= REGISTER =================
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            try
            {
                var userId = await _sender.Send(command);

                var emailSent = await _mediator.Send(
                    new ResendConfirmationCommand { Email = command.Email });

                return Ok(new
                {
                    message = emailSent
                        ? "Đăng ký thành công!"
                        : "Đăng ký thành công nhưng không thể gửi email xác thực.",
                    userId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            try
            {
                var token = await _sender.Send(command);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================= GOOGLE LOGIN =================
        [HttpPost("google-login")]
        [AllowAnonymous]
        [DisableRateLimiting]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.IdToken))
                return BadRequest(new { message = "Google ID token là bắt buộc." });

            try
            {
                var token = await _sender.Send(command);
                return Ok(new { token });
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

        // ================= FORGOT / RESET PASSWORD =================
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            try
            {
                await _sender.Send(command);
                return Ok(new { message = "Vui lòng kiểm tra email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _sender.Send(command);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================= CHANGE PASSWORD =================
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            try
            {
                var result = await _sender.Send(command);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================= EMAIL CONFIRM =================
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [DisableRateLimiting]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _mediator.Send(command);
                return Ok(new { message = result, success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    success = false
                });
            }
        }

        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        [DisableRateLimiting]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _sender.Send(command);
                return Ok(new
                {
                    message = result
                        ? "Email xác thực đã được gửi."
                        : "Không thể gửi email xác thực. Vui lòng thử lại sau.",
                    success = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================= ENTERPRISE =================
        [HttpPost("register-enterprise")]
        public async Task<IActionResult> RegisterEnterprise([FromBody] RegisterEnterpriseCommand command)
        {
            try
            {
                var id = await _sender.Send(command);
                return Ok(new { message = "Đăng ký doanh nghiệp thành công!", enterpriseId = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-hr-account")]
        public async Task<IActionResult> CreateHRAccount([FromBody] CreateHRAccountCommand command)
        {
            try
            {
                var userId = await _sender.Send(command);
                var emailSent = await _mediator.Send(
                    new ResendConfirmationCommand { Email = command.Email });

                return Ok(new
                {
                    message = emailSent
                        ? "Tạo tài khoản thành công! Vui lòng kiểm tra email."
                        : "Tạo tài khoản thành công nhưng không thể gửi email xác thực.",
                    userId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
