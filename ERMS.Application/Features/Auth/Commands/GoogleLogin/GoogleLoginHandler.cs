using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
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
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;


        public GoogleLoginHandler(
            UserManager<User> userManager,
            ITokenService tokenService,
            IConfiguration configuration,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _configuration = configuration;
            _roleManager = roleManager;
        }
        private const string DefaultRole = AppRoles.Candidate;

        public async Task<string> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    EmailConfirmed = true
                };

                await _userManager.CreateAsync(user);

                if (!await _roleManager.RoleExistsAsync(DefaultRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRole));
                }

                await _userManager.AddToRoleAsync(user, DefaultRole);
            }

            return await _tokenService.CreateToken(user);
        }
    }
}
