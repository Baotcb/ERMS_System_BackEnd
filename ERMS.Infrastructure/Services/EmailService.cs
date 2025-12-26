using ERMS.Application.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();

            // 1. Người gửi
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));

            // 2. Người nhận
            email.To.Add(MailboxAddress.Parse(toEmail));

            // 3. Nội dung
            email.Subject = subject;
            var builder = new BodyBuilder();
            builder.HtmlBody = body; // Hỗ trợ gửi HTML
            email.Body = builder.ToMessageBody();

            // 4. Kết nối SMTP Gmail và gửi
            using var smtp = new SmtpClient();
            try
            {
                // Kết nối đến server Gmail (Port 587 là chuẩn cho TLS)
                await smtp.ConnectAsync(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:Port"]), SecureSocketOptions.StartTls);

                // Đăng nhập
                await smtp.AuthenticateAsync(_config["EmailSettings:SenderEmail"], _config["EmailSettings:Password"]);

                
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
