using MediatR;
using System;

namespace ERMS.Application.Features.Auth.Commands.CreateHRAccount
{
    public class CreateHRAccountCommand : IRequest<Guid>
    {
        public Guid EnterpriseId { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }
}
