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
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ILogger<GoogleLoginHandler> _logger;

        private const string DefaultRole = AppRoles.Candidate;

        public GoogleLoginHandler(
            UserManager<User> userManager,
            ITokenService tokenService,
            IConfiguration configuration,
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger<GoogleLoginHandler> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _configuration = configuration;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<string> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                _logger.LogWarning("Google login attempt with empty ID token");
                throw new UnauthorizedAccessException("Google ID token không hợp lệ.");
            }

            // Validate Google ID token
            GoogleJsonWebSignature.Payload? payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                
                if (payload == null)
                {
                    _logger.LogWarning("Google token validation returned null payload");
                    throw new UnauthorizedAccessException("Không thể xác thực token Google.");
                }
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID token");
                throw new UnauthorizedAccessException("Google ID token không hợp lệ hoặc đã hết hạn.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google ID token");
                throw new UnauthorizedAccessException("Lỗi xác thực Google. Vui lòng thử lại.");
            }

            // Validate email
            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                _logger.LogWarning("Google token missing email claim");
                throw new UnauthorizedAccessException("Email không được cung cấp từ Google.");
            }

            // Check if email is verified
            if (!payload.EmailVerified)
            {
                _logger.LogWarning("Google email not verified for {Email}", payload.Email);
                throw new UnauthorizedAccessException("Email chưa được xác thực bởi Google.");
            }

            // Find or create user
            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                // Create new user
                _logger.LogInformation("Creating new user from Google login: {Email}", payload.Email);
                
                user = new User
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FullName = payload.Name ?? payload.Email.Split('@')[0],
                    EmailConfirmed = true,
                    DateJoined = DateTime.UtcNow,
                    AvatarUrl = payload.Picture
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user from Google login: {Errors}", errors);
                    throw new Exception($"Không thể tạo tài khoản: {errors}");
                }

                // Ensure default role exists
                if (!await _roleManager.RoleExistsAsync(DefaultRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRole));
                }

                // Assign default role
                var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to assign default role to new user: {Errors}", 
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                _logger.LogInformation("Successfully created user from Google login: {UserId}, {Email}", 
                    user.Id, user.Email);
            }
            else
            {
                // Update existing user info if needed
                var updated = false;
                
                if (!string.IsNullOrWhiteSpace(payload.Name) && user.FullName != payload.Name)
                {
                    user.FullName = payload.Name;
                    updated = true;
                }

                if (!string.IsNullOrWhiteSpace(payload.Picture) && user.AvatarUrl != payload.Picture)
                {
                    user.AvatarUrl = payload.Picture;
                    updated = true;
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    updated = true;
                }

                if (updated)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to update user info from Google login: {Errors}", 
                            string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                }

                _logger.LogInformation("User logged in via Google: {UserId}, {Email}", user.Id, user.Email);
            }

            // Generate JWT token
            return await _tokenService.CreateToken(user);
        }
    }
}
