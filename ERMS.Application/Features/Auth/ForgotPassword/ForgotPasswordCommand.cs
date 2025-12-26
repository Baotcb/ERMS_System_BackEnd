using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ERMS.Application.Features.Auth.ForgotPassword
{
    public class ForgotPasswordCommand : IRequest<string>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
