using MediatR;

namespace ERMS.Application.Features.Users.Commands.LockUser
{
    public sealed class LockUserCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public bool IsLocked { get; set; } // true = lock, false = unlock
    }
}
