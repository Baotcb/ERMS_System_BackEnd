using MediatR;

namespace ERMS.Application.Features.Auth.Commands.GoogleLogin
{
    public class GoogleLoginCommand : IRequest<string>
    {
        public string IdToken { get; set; }
    }
}
