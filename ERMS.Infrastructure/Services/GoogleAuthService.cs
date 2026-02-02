using ERMS.Application.Features.Auth.Commands.GoogleLogin;
using ERMS.Application.Features.Auth.Commands;
using ERMS.Application.Interface;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _config;

        public GoogleAuthService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["GoogleAuth:ClientId"] }
                });

            if (payload == null)
                throw new UnauthorizedAccessException("Google token không hợp lệ.");

            return new GoogleUserInfo
            {
                Email = payload.Email,
                Name = payload.Name,
                Subject = payload.Subject,
                Picture = payload.Picture,
                EmailVerified = payload.EmailVerified
            };
        }
    }
}
    
