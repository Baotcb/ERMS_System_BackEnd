using ERMS.Application.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ERMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        private sealed class EmailSettings
        {
            public string? SmtpServer { get; init; }
            public int Port { get; init; }
            public string? SenderName { get; init; }
            public string? SenderEmail { get; init; }
            public string? Password { get; init; }
        }

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = GetEmailSettings();
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                emailSettings.SenderName,
                emailSettings.SenderEmail));

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(
                    emailSettings.SmtpServer,
                    emailSettings.Port,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    emailSettings.SenderEmail,
                    emailSettings.Password);

                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, Dictionary<string, byte[]> attachments)
        {
            var emailSettings = GetEmailSettings();
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                emailSettings.SenderName,
                emailSettings.SenderEmail));

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = body;

        
            foreach (var attachment in attachments)
            {
                builder.Attachments.Add(attachment.Key, attachment.Value);
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(
                    emailSettings.SmtpServer,
                    emailSettings.Port,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    emailSettings.SenderEmail,
                    emailSettings.Password);

                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }

        private EmailSettings GetEmailSettings()
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var senderName = _config["EmailSettings:SenderName"];
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:Password"]?.Trim();

            if (smtpServer != null && smtpServer.Contains("gmail.com", StringComparison.OrdinalIgnoreCase) && password != null)
            {
                password = string.Concat(password.Where(ch => !char.IsWhiteSpace(ch)));
            }

            return new EmailSettings
            {
                SmtpServer = smtpServer,
                Port = int.Parse(_config["EmailSettings:Port"] ?? "587"),
                SenderName = senderName,
                SenderEmail = senderEmail,
                Password = password
            };
        }
    }
}
