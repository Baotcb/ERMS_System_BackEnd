using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace ERMS.Application.Features.Auth.Commands.GoogleLogin
{
    public class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public GoogleLoginHandler(
            UserManager<User> userManager,
            ITokenService tokenService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        public async Task<string> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify Google ID Token
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                });

            if (payload == null)
            {
                throw new Exception("Google token không hợp lệ.");
            }

            // 2. Tìm user theo email
            var user = await _userManager.FindByEmailAsync(payload.Email);

            // 3. Nếu chưa có user → tạo mới
            if (user == null)
            {
                user = new User
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FullName = payload.Name,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    throw new Exception("Không thể tạo tài khoản Google.");
                }
            }

            // 4. Tạo JWT token
            return await _tokenService.CreateToken(user);
        }
    }
}
