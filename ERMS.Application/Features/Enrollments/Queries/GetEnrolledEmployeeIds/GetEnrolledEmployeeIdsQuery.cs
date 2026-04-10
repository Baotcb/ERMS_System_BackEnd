using MediatR;

namespace ERMS.Application.Features.Enrollments.Queries.GetEnrolledEmployeeIds;

public sealed class GetEnrolledEmployeeIdsQuery : IRequest<List<Guid>>
{
    public Guid CourseId { get; set; }
}
