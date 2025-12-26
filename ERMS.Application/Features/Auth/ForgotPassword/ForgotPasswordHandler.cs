using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Auth.ForgotPassword
{
    public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        public ForgotPasswordHandler(UserManager<User> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }
        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                
                return "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
            }

           
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

          
            string emailBody = $@"
            <h3>Yêu cầu đặt lại mật khẩu - ERMS System</h3>
            <p>Chào {user.FullName},</p>
            <p>Bạn vừa yêu cầu đặt lại mật khẩu. Vui lòng sử dụng Token dưới đây:</p>
            <p><b>{token}</b></p>
            <p>Token này sẽ hết hạn sau vài giờ.</p>
            <br/>
            <p>Trân trọng,<br/>Đội ngũ quản trị</p>
        ";

            
            await _emailService.SendEmailAsync(user.Email, "Reset Password Token", emailBody);

            return "Email hướng dẫn đã được gửi đi!";
        }
    }
}
