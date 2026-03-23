using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Auth.Commands.Register
{
    public class RegisterHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IERMSDbContext _context;
        private readonly ILogger<RegisterHandler> _logger;

        public RegisterHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IERMSDbContext context,
            ILogger<RegisterHandler> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Email đã tồn tại trong hệ thống.");
            }

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                DateJoined = DateTime.UtcNow,
                SecurityStamp = Guid.CreateVersion7().ToString()
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Đăng ký không thành công: {errors}");
            }

            try
            {
                await EnsureCandidateRoleExistsAsync();

                var addRoleResult = await _userManager.AddToRoleAsync(user, AppRoles.Candidate);
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Không thể gán vai trò ứng viên: {errors}");
                }

                var candidate = new Candidate
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Candidates.Add(candidate);
                await _context.SaveChangesAsync(cancellationToken);

                return user.Id;
            }
            catch
            {
                await CleanupCreatedUserAsync(user);
                throw;
            }
        }

        private async Task EnsureCandidateRoleExistsAsync()
        {
            if (await _roleManager.RoleExistsAsync(AppRoles.Candidate))
            {
                return;
            }

            var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Candidate));
            if (createRoleResult.Succeeded || await _roleManager.RoleExistsAsync(AppRoles.Candidate))
            {
                return;
            }

            var errors = string.Join(", ", createRoleResult.Errors.Select(e => e.Description));
            throw new Exception($"Không thể tạo vai trò ứng viên: {errors}");
        }

        private async Task CleanupCreatedUserAsync(User user)
        {
            if (user.Id == Guid.Empty)
            {
                return;
            }

            try
            {
                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to cleanup partially created candidate user {Email}. Errors: {Errors}",
                        user.Email,
                        errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to cleanup partially created candidate user {Email}",
                    user.Email);
            }
        }
    }
}
