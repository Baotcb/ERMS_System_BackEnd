using ERMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ERMS.Application.Interface;
using Microsoft.Extensions.Logging;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Application.Features.Auth.Commands.ResendConfirmation
{
    public class ResendConfirmationHandler : IRequestHandler<ResendConfirmationCommand, bool>
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<ResendConfirmationHandler> _logger;

        public ResendConfirmationHandler(
            UserManager<User> userManager,
            IEmailService emailService,
            IConfiguration config,
            ILogger<ResendConfirmationHandler> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    // Không tiết lộ email có tồn tại hay không (security best practice)
                    _logger.LogWarning("Resend confirmation attempted for non-existent email: {Email}", request.Email);
                    return true;
                }

                if (user.EmailConfirmed)
                {
                    throw new InvalidOperationException("Email đã được xác thực trước đó.");
                }

                // Tạo email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var clientUrl = _config["ClientSettings:Url"];
                var encodedToken = Uri.EscapeDataString(token);
                var confirmationUrl = $"{clientUrl}/auth/confirm-email?userId={user.Id}&token={encodedToken}";

                // Template email
                var subject = "Xác thực email tài khoản ERMS";
                var body = CreateEmailTemplate(user.FullName, confirmationUrl);

                // Gửi email
                await _emailService.SendEmailAsync(user.Email!, subject, body);

                _logger.LogInformation("Email confirmation sent to: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email confirmation to: {Email}", request.Email);
                throw;
            }
        }

        private static string CreateEmailTemplate(string fullName, string confirmationUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Xác thực Email - ERMS</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #fff; border-radius: 10px; margin-top: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ padding: 30px 20px; }}
        .button {{ display: inline-block; background-color: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 5px; margin-top: 15px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Xác Thực Email</h1>
            <h2>ERMS - Employee Resource Management System</h2>
        </div>
        
        <div class='content'>
            <h3>Xin chào {fullName}!</h3>
            
            <p>Cảm ơn bạn đã đăng ký tài khoản ERMS. Để hoàn tất quá trình đăng ký và bảo mật tài khoản, vui lòng xác thực địa chỉ email của bạn.</p>
            
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{confirmationUrl}' class='button'>✅ Xác Thực Email Ngay</a>
            </p>
            
            <div class='warning'>
                <p><strong>⚠️ Quan trọng:</strong></p>
                <ul>
                    <li>Link này sẽ hết hạn sau <strong>24 giờ</strong></li>
                    <li>Nếu không xác thực email, bạn sẽ không thể đăng nhập vào hệ thống</li>
                    <li>Nếu link không hoạt động, hãy copy đường dẫn sau vào trình duyệt:</li>
                </ul>
                <p style='word-break: break-all; background: #f8f9fa; padding: 10px; border-radius: 3px; font-family: monospace; font-size: 11px;'>
                    {confirmationUrl}
                </p>
            </div>
            
            <p style='margin-top: 30px;'>
                Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email này hoặc liên hệ với chúng tôi.
            </p>
        </div>
        
        <div class='footer'>
            <p>Email này được gửi tự động từ hệ thống ERMS.</p>
            <p>Vui lòng không trả lời email này.</p>
            <hr>
            <p>&copy; 2026 ERMS System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}