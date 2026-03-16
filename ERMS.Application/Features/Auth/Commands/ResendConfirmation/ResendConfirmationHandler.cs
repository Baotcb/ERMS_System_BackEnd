using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new InvalidOperationException("Email không tồn tại trong hệ thống.");
            }

            if (user.EmailConfirmed)
            {
                throw new InvalidOperationException("Email đã được xác thực trước đó.");
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var clientUrl = _config["ClientSettings:Url"];
                if (string.IsNullOrWhiteSpace(clientUrl) || !Uri.TryCreate(clientUrl, UriKind.Absolute, out var clientUri))
                {
                    _logger.LogError("Cannot send email confirmation because ClientSettings:Url is invalid: {ClientUrl}", clientUrl);
                    return false;
                }

                var encodedToken = Uri.EscapeDataString(token);
                var confirmationUrl =
                    $"{clientUrl.TrimEnd('/')}/confirm-email?userId={user.Id}&token={encodedToken}&email={Uri.EscapeDataString(user.Email!)}";

                var subject = "Xác thực email tài khoản ERMS";
                var body = CreateEmailTemplate(user.FullName, confirmationUrl);

                await _emailService.SendEmailAsync(user.Email!, subject, body);

                _logger.LogInformation("Email confirmation sent successfully to: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email confirmation to: {Email}", request.Email);
                return false;
            }
        }

        private static string CreateEmailTemplate(string fullName, string confirmationUrl)
        {
            return $@"
<!DOCTYPE html>
<html lang='vi'>

<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Xác thực Email - ERMS</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');

        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 0;
            background-color: #f6f8ff;
            color: #1a1e36;
            -webkit-font-smoothing: antialiased;
        }}

        .wrapper {{
            width: 100%;
            background-color: #f6f8ff;
            padding: 60px 20px;
        }}

        .container {{
            max-width: 560px;
            margin: 0 auto;
            background-color: #ffffff;
            border: 1px solid #e0e4f2;
            border-radius: 20px;
            overflow: hidden;
            box-shadow: 0 10px 25px rgba(79, 70, 229, 0.05);
        }}

        .accent-bar {{
            height: 6px;
            background: linear-gradient(90deg, #4f46e5 0%, #7c3aed 100%);
        }}

        .brand-section {{
            padding: 40px 45px 15px 45px;
            text-align: left;
        }}

        .brand-logo {{
            font-size: 32px;
            font-weight: 700;
            color: #7c3aed;
            letter-spacing: 1px;
        }}

        .logo-icon {{
            display: none;
        }}

        .main-content {{
            padding: 15px 45px 45px 45px;
        }}

        .title {{
            font-size: 26px;
            font-weight: 700;
            color: #1e1b4b;
            margin: 0 0 18px 0;
            letter-spacing: -0.5px;
        }}

        .greeting {{
            font-size: 17px;
            font-weight: 600;
            color: #312e81;
            margin-bottom: 12px;
        }}

        .text {{
            font-size: 15px;
            color: #4b5563;
            margin-bottom: 30px;
        }}

        .cta-section {{
            text-align: center;
            margin: 35px 0;
        }}

        .button {{
            display: inline-block;
            background-color: #4f46e5;
            background: linear-gradient(135deg, #4f46e5 0%, #6366f1 100%);
            color: #ffffff !important;
            -webkit-text-fill-color: #ffffff;
            padding: 14px 34px;
            text-decoration: none;
            border-radius: 12px;
            font-weight: 600;
            font-size: 16px;
            box-shadow: 0 4px 15px rgba(79, 70, 229, 0.2);
            border: 1px solid #4f46e5;
        }}

        .expiry-text {{
            display: block;
            font-size: 12px;
            color: #94a3b8;
            margin-top: 15px;
        }}

        .security-note {{
            background-color: #f8fafc;
            border-radius: 12px;
            padding: 10px 14px;
            font-size: 13px;
            color: #64748b;
            border-left: 4px solid #e2e8f0;
        }}

        .security-item {{
            margin-bottom: 0;
            display: flex;
            align-items: flex-start;
            gap: 12px;
        }}

        .icon {{
            color: #94a3b8;
            font-size: 14px;
        }}

        .footer {{
            text-align: center;
            padding: 40px 45px;
            background-color: #fcfdfe;
            border-top: 1px solid #f1f5f9;
        }}

        .footer-text {{
            font-size: 12px;
            color: #94a3b8;
            line-height: 2;
        }}

        .footer-links {{
            margin: 15px 0 25px 0;
        }}

        .footer-links a {{
            color: #4f46e5;
            text-decoration: none;
            font-weight: 600;
            padding: 0 10px;
        }}

        .copyright {{
            font-size: 11px;
            color: #cbd5e1;
            font-weight: 500;
        }}
    </style>
</head>

<body>
    <div class='wrapper'>
        <div class='container'>
            <div class='accent-bar'></div>
            <div class='brand-section'>
                <div class='brand-logo'>ERMS</div>
            </div>

            <div class='main-content'>
                <h1 class='title'>Xác thực tài khoản</h1>

                <p class='greeting'>Chào {fullName},</p>

                <p class='text'>
                    Chào mừng bạn đến với ERMS! Chúng tôi rất vui khi bạn gia nhập cộng đồng quản trị nhân sự hiện đại.
                    Để bắt đầu sử dụng đầy đủ tính năng, vui lòng xác nhận địa chỉ email của bạn.
                </p>

                <div class='cta-section'>
                    <a href='{confirmationUrl}' class='button'>Xác thực ngay</a>
                    <span class='expiry-text'>Liên kết hết hạn sau 24 giờ</span>
                </div>

                <div class='security-note'>
                    <div class='security-item'>
                        <span class='icon'>ⓘ</span>
                        <span>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</span>
                    </div>
                </div>

                <p style='margin-top: 35px; font-size: 14px; color: #64748b;'>
                    Trân trọng,<br>
                    <strong style='color: #4f46e5;'>Đội ngũ ERMS</strong>
                </p>
            </div>

            <div class='footer'>
                <div class='footer-text'>
                    Bạn nhận được email này vì đã đăng ký tài khoản tại ERMS.<br>
                    Vui lòng không phản hồi trực tiếp vào địa chỉ này.
                </div>
                <div class='footer-links'>
                    <a href='#'>Hướng dẫn</a>
                    <a href='#'>Trung tâm hỗ trợ</a>
                    <a href='#'>Bảo mật</a>
                </div>
                <div class='copyright'>&copy; 2026 ERMS System. Built with trust and security.</div>
            </div>
        </div>
    </div>
</body>

</html>";
        }
    }
}
