using ERMS.Application.Features.Users.DTO;
using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Users.Commands.GetProfile
{
    public class GetProfileHandler : IRequestHandler<GetProfileCommand, UserProfileDto>
    {
        private readonly UserManager<User> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IERMSDbContext _dbContext;

        public GetProfileHandler(UserManager<User> userManager, ICurrentUserService currentUserService, IERMSDbContext dbContext)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<UserProfileDto> Handle(GetProfileCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong Token.");
            }

            var user = await _userManager.Users
                .Include(u => u.Department).ThenInclude(d => d.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                throw new Exception("Người dùng không tồn tại trong hệ thống.");
            }

            // Fallback for DepartmentId from the Employees table if missing in AspNetUsers
            var employee = await _dbContext.Employees
                .Include(e => e.Department).ThenInclude(d => d.Enterprise)
                .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);

            return new UserProfileDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                Hometown = user.Hometown,
                Phones = user.PhoneNumber,
                DepartmentId = user.DepartmentId ?? employee?.DepartmentId,
                DepartmentName = user.Department?.DepartmentName ?? employee?.Department?.DepartmentName,
                EnterpriseName = user.Department?.Enterprise?.EnterpriseName ?? employee?.Department?.Enterprise?.EnterpriseName,
                EnterpriseLogoUrl = user.Department?.Enterprise?.LogoUrl ?? employee?.Department?.Enterprise?.LogoUrl,
                AvatarUrl = user.AvatarUrl,
                DateJoined = user.DateJoined
            };
        }
    }
}
