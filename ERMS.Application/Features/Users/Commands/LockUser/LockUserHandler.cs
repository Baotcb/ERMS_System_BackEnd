using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ERMS.Application.Features.Users.Commands.LockUser
{
    public sealed class LockUserHandler : IRequestHandler<LockUserCommand, bool>
    {
        private readonly UserManager<User> _userManager;

        public LockUserHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> Handle(LockUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null) return false;

            if (request.IsLocked)
            {
                // Lock user permanently
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
            else
            {
                // Unlock user
                await _userManager.SetLockoutEndDateAsync(user, null);
            }

            return true;
        }
    }
}
