using MediatR;
using System;

namespace ERMS.Application.Features.Enrollments.Commands.UpdateAttendance
{
    public sealed class UpdateAttendanceCommand : IRequest<Guid>
    {
        public Guid EnrollmentId { get; set; }
        public string Status { get; set; } = null!;
    }
}