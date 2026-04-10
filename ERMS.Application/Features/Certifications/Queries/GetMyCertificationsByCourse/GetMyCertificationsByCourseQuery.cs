using MediatR;

namespace ERMS.Application.Features.Certifications.Queries
{
    public sealed class GetMyCertificationsByCourseQuery : IRequest<List<CertificationDto>>
    {
        public Guid CourseId { get; set; }
    }
}