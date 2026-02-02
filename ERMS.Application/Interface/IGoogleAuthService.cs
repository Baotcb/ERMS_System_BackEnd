using ERMS.Application.Features.Auth.Commands.GoogleLogin;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Interface
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken);
    }

}
