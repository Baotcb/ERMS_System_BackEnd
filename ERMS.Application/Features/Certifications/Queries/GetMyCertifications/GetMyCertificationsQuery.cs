using MediatR;

namespace ERMS.Application.Features.Certifications.Queries.GetMyCertifications
{
    public sealed class GetMyCertificationsQuery : IRequest<List<CertificationDto>>
    {
    }
}