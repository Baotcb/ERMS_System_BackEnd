using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Auth.Commands.GoogleLogin
{
    public class GoogleUserInfo
    {
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Picture { get; set; } = null!;
        public bool EmailVerified { get; set; }
    }

}
