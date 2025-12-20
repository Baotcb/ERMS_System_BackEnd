using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Auth.Login
{
    public class LoginCommand : IRequest<string>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
