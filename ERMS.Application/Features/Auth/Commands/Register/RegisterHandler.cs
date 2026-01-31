
using ERMS.Application.Features.Auth.Commands.ResendConfirmation;
using ERMS.Application.Features.Auth.Commands.Register;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Auth.Commands.Register
{
    public class RegisterHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IERMSDbContext _context;
        private readonly IMediator _mediator;
        public RegisterHandler(UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IERMSDbContext context,
            IMediator mediator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _mediator = mediator;
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
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Đăng ký không thành công: {errors}");
            }


            if (await _roleManager.RoleExistsAsync(AppRoles.Candidate.ToString()))
            {
                await _userManager.AddToRoleAsync(user, AppRoles.Candidate.ToString());

            }
            else
            {
                await _userManager.AddToRoleAsync(user, AppRoles.Candidate);
            }
            var c = new Candidate
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Candidates.Add(c);
            await _context.SaveChangesAsync(cancellationToken);
            try
            {
                await _mediator.Send(new ResendConfirmationCommand { Email = user.Email }, cancellationToken);
            }
            catch (Exception)
            {
              
            }
    
            return user.Id;
        }

    }
}
