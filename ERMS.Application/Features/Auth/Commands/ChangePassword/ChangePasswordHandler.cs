using ERMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Auth.Commands.ChangePassword
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, string>
    {
        private readonly UserManager<User> _userManager;
        public ChangePasswordHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        public Task<string> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = _userManager.FindByIdAsync(request.UserId.ToString()).Result;
            if (user == null)
            {
                throw new Exception("Không tìm thấy người dùng");
            }
            var result = _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword).Result;
            if (!result.Succeeded)
            {
                throw new Exception("Thay đổi mật khẩu thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            return Task.FromResult("Thay đổi mật khẩu thành công");
        }
    }
}
