using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Auth.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        public LoginHandler(UserManager<User> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
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

            return await _tokenService.CreateToken(user);
        }
    }
}
