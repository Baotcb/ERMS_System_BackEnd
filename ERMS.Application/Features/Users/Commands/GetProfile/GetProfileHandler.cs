using ERMS.Application.Features.Users.DTO;
using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Users.Commands.GetProfile
{
    public class GetProfileHandler : IRequestHandler<GetProfileCommand, UserProfileDto>
    {
        private readonly UserManager<User> _userManager;
        private readonly ICurrentUserService _currentUserService;


        public GetProfileHandler(UserManager<User> userManager, ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
        }
        public async Task<UserProfileDto> Handle(GetProfileCommand request, CancellationToken cancellationToken)
        {
            
            var userId = _currentUserService.UserId;

            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong Token.");
            }

     
            var user = await _userManager.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                throw new Exception("Người dùng không tồn tại trong hệ thống.");
            }

          
            var roles = await _userManager.GetRolesAsync(user);

         
            return new UserProfileDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                Hometown = user.Hometown,
                Phones = user.Phones,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.DepartmentName,
                Status = user.Status,
                DateJoined = user.DateJoined
            };
        }
    }
}
