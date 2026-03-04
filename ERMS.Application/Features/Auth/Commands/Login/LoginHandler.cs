using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Auth.Commands.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IERMSDbContext _context;
        public LoginHandler(UserManager<User> userManager, ITokenService tokenService,IERMSDbContext context)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
        }
        public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken) 
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new Exception("Tài khoản hoặc mật khẩu không đúng.");
            }

            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
            {
                throw new Exception("Tài khoản hoặc mật khẩu không đúng.");
            }
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            if (role.ToString() != AppRoles.Candidate.ToString() && role.ToString() != AppRoles.Admin)
            {
                var enterpriseId = _context.Employees.Where(e => e.UserId == user.Id).Select(e => e.EnterpriseId).FirstOrDefault();
                var enterprise = _context.Enterprises.Find(enterpriseId);
                if (enterprise.Status == Domain.Constants.Enterprise.EnterpriseStatus.Locked)
                {
                    throw new Exception("Tài khoản doanh nghiệp đã bị khóa. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");
                }
                if(enterprise.Status == Domain.Constants.Enterprise.EnterpriseStatus.Suspended)
                {
                    throw new Exception("Tài khoản doanh nghiệp của bạn đang chờ phê duyệt. Vui lòng chờ quản trị viên phê duyệt tài khoản của bạn.");
                }
                if(enterprise.Status == Domain.Constants.Enterprise.EnterpriseStatus.Inactive)
                {
                    throw new Exception("Tài khoản doanh nghiệp không hoạt động. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");
                }
            }

            

            return await _tokenService.CreateToken(user);
        }
    }
}
