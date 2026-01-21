using ERMS.Application.Features.Auth.Commands.ChangePassword;
using ERMS.Application.Features.Auth.Commands.ForgotPassword;
using ERMS.Application.Features.Auth.Commands.GoogleLogin;
using ERMS.Application.Features.Auth.Commands.Login;
using ERMS.Application.Features.Auth.Commands.Register;
using ERMS.Application.Features.Auth.Commands.ResetPassword;
using ERMS.Application.Interface;
using ERMS.Domain.Entities;
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
using System.IdentityModel.Tokens.Jwt;
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

        public AuthController(ISender sender,
            IConfiguration config,
            ITokenService tokenService,
            IMediator mediator)
        {
            _sender = sender;
            _config = config;
            _tokenService = tokenService;
            _mediator = mediator;
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

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Google callback
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(
                IdentityConstants.ExternalScheme);

            if (!result.Succeeded)
                return Unauthorized();

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            var token = await _mediator.Send(new GoogleLoginCommand
            {
                Email = email!,
                FullName = name
            });

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            return Ok(new { token });
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

    } 
}

