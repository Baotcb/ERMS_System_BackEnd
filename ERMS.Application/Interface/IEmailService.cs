using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Interface
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
