using MediatR;

namespace ERMS.Application.Features.Auth.Commands.ResendConfirmation
{
    public class ResendConfirmationCommand : IRequest<bool>
    {
        public string Email { get; set; } = string.Empty;
    }
}