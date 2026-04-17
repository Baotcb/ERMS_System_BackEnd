using MediatR;
using Microsoft.AspNetCore.Http;

namespace ERMS.Application.Features.Applications.Commands.ExtractCVInfo;

public sealed class ExtractCVInfoCommand : IRequest<ExtractCVInfoResult>
{
    public IFormFile CvFile { get; set; } = null!;
}
