using MediatR;

namespace ERMS.Application.Features.Enrollments.Commands.UpdateCertificate
{
    public class UpdateCertificateCommand : IRequest<bool>
    {
        public Guid EnrollmentId { get; set; }
        public string CertificateUrl { get; set; } = null!;
    }
}