using ERMS.Application.Features.Users.DTO;
using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Users.Commands.ChangeProfile
{
    public class ChangeProfileHandler : IRequestHandler<ChangeProfileCommand, UserProfileDto>
    {
        private readonly UserManager<User> _userManager;
        private readonly ICurrentUserService _currentUserService;
        public ChangeProfileHandler(UserManager<User> userManager, ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
        }
        public async Task<UserProfileDto> Handle(ChangeProfileCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong Token.");
            }
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new UnauthorizedAccessException("Người dùng không tồn tại.");
            }
            user.FullName = request.FullName;
            user.DateOfBirth = request.DateOfBirth;
            user.Hometown = request.Hometown;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Cập nhật profile thất bại: {errors}");
            }
            return new UserProfileDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                Hometown = user.Hometown,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.DepartmentName,
                DateJoined = user.DateJoined
            };
        }

    }
}
