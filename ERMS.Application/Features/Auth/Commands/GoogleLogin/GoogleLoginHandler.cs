using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Auth.Commands.GoogleLogin
{
    public class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IERMSDbContext _context;
        private readonly IGoogleAuthService _googleAuthService;

        private const string Provider = "Google";
        private const string DefaultRole = AppRoles.Candidate;

        public GoogleLoginHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ITokenService tokenService,
            IGoogleAuthService googleAuthService,
            IERMSDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _googleAuthService = googleAuthService;
            _context = context;
        }

        public async Task<string> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ Validate Google token (delegate cho Infrastructure)
            var payload = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);

            if (!payload.EmailVerified)
                throw new UnauthorizedAccessException("Email Google chưa được xác thực.");

            // 2️⃣ Tạo LoginInfo cho Google
            var loginInfo = new UserLoginInfo(
                Provider,
                payload.Subject,
                Provider
            );

            // 3️⃣ Tìm user theo external login
            var user = await _userManager.FindByLoginAsync(
                loginInfo.LoginProvider,
                loginInfo.ProviderKey);

            // 4️⃣ Nếu chưa có → tìm theo email
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(payload.Email);

                if (user != null)
                {
                    var linkResult = await _userManager.AddLoginAsync(user, loginInfo);
                    if (!linkResult.Succeeded)
                        throw new Exception("Không thể liên kết Google account.");
                }
            }

            // 5️⃣ Nếu chưa tồn tại user → tạo mới
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

                await _userManager.AddLoginAsync(user, loginInfo);

                if (!await _roleManager.RoleExistsAsync(DefaultRole))
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRole));

                await _userManager.AddToRoleAsync(user, DefaultRole);

                var candidate = new Candidate
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Candidates.Add(candidate);
                await _context.SaveChangesAsync(cancellationToken);
            }

           
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            if (role != null && role != AppRoles.Candidate && role != AppRoles.Admin)
            {
                var enterpriseId = _context.Employees
                    .Where(e => e.UserId == user.Id)
                    .Select(e => e.EnterpriseId)
                    .FirstOrDefault();

                if (enterpriseId != Guid.Empty)
                {
                    var enterprise = await _context.Enterprises.FindAsync(enterpriseId);
                    
                    if (enterprise != null)
                    {
                        if (enterprise.Status == Domain.Constants.Enterprise.EnterpriseStatus.Locked)
                        {
                            throw new Exception("Tài khoản doanh nghiệp đã bị khóa. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");
                        }
                        if (enterprise.Status == Domain.Constants.Enterprise.EnterpriseStatus.Suspended)
                        {
                            throw new Exception("Tài khoản doanh nghiệp của bạn đang chờ phê duyệt. Vui lòng chờ quản trị viên phê duyệt tài khoản của bạn.");
                        }
                        if (enterprise.Status == Domain.Constants.Enterprise.EnterpriseStatus.Inactive)
                        {
                            throw new Exception("Tài khoản doanh nghiệp không hoạt động. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");
                        }
                    }
                }
            }

            // 6️⃣ Generate JWT
            return await _tokenService.CreateToken(user);
        }
    }
}
