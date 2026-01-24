using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Identity;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ERMS.Application.Features.Auth.Commands.GoogleLogin
{
    public class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        private const string Provider = "Google";
        private const string DefaultRole = AppRoles.Candidate;

        public GoogleLoginHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ITokenService tokenService,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _config = config;
        }

        public async Task<string> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ Validate Google token
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["GoogleAuth:ClientId"] }
                });

            if (payload == null || !payload.EmailVerified)
                throw new UnauthorizedAccessException("Google token không hợp lệ.");

            // 2️⃣ Tạo LoginInfo
            var loginInfo = new UserLoginInfo(
                Provider,
                payload.Subject, // sub
                Provider
            );

            // 3️⃣ ƯU TIÊN tìm theo provider
            var user = await _userManager.FindByLoginAsync(
                loginInfo.LoginProvider,
                loginInfo.ProviderKey);

            // 4️⃣ Nếu chưa có → tìm theo email
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(payload.Email);

                if (user != null)
                {
                    // 🔗 Link Google vào user cũ (đăng ký bằng password)
                    var linkResult = await _userManager.AddLoginAsync(user, loginInfo);
                    if (!linkResult.Succeeded)
                        throw new Exception("Không thể liên kết Google account.");
                }
            }

            // 5️⃣ Nếu email cũng chưa tồn tại → tạo user mới
            if (user == null)
            {
                user = new User
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FullName = payload.Name ?? payload.Email.Split('@')[0],
                    AvatarUrl = payload.Picture,
                    EmailConfirmed = true,
                    DateJoined = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    throw new Exception(string.Join(", ",
                        createResult.Errors.Select(e => e.Description)));
                }

                // 🔗 Link Google
                await _userManager.AddLoginAsync(user, loginInfo);

                // Role mặc định
                if (!await _roleManager.RoleExistsAsync(DefaultRole))
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRole));

                await _userManager.AddToRoleAsync(user, DefaultRole);
            }

            // 6️⃣ Generate JWT
            return await _tokenService.CreateToken(user);
        }
    }

}
