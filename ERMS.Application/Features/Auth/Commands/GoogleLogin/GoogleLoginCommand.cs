using MediatR;

namespace ERMS.Application.Features.Auth.Commands.GoogleLogin
{
    public class GoogleLoginCommand : IRequest<string>
    {
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
    }
}
