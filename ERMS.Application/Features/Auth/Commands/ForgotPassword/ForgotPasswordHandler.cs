using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public ForgotPasswordHandler(UserManager<User> userManager, IEmailService emailService, IConfiguration configuration)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return "Email đã được gửi.";

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(user.Email);

            string clientUrl = _configuration["ClientSettings:Url"];
            string resetLink = $"{clientUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

            string emailBody = $@"
        <h3>Yêu cầu đặt lại mật khẩu</h3>
        <p>Chào {user.FullName},</p>
        <p>Bấm vào nút bên dưới để đặt lại mật khẩu của bạn:</p>
        <a href='{resetLink}' 
           style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
           Đặt lại mật khẩu
        </a>
        <p>Hoặc copy link này vào trình duyệt:</p>
        <p>{resetLink}</p>
    ";

            await _emailService.SendEmailAsync(user.Email, "Reset Password", emailBody);

            return "Email đã được gửi đi!";
        }
    }
}