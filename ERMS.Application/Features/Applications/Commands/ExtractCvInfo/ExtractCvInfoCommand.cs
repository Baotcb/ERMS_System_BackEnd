using ERMS.Application.Interface;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ERMS.Application.Features.Applications.Commands.ExtractCvInfo;

public sealed class ExtractCvInfoCommand : IRequest<ExtractCvInfoResult>
{
    public IFormFile CvFile { get; set; } = null!;
}
