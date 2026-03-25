using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Enrollments.Queries.GetEnrolledEmployeeIds;

public sealed class GetEnrolledEmployeeIdsHandler
    : IRequestHandler<GetEnrolledEmployeeIdsQuery, List<Guid>>
{
    private readonly IERMSDbContext _context;

    public GetEnrolledEmployeeIdsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> Handle(
        GetEnrolledEmployeeIdsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeIds = await _context.Enrollments
            .Where(e => e.CourseId == request.CourseId && !e.IsDeleted)
            .Select(e => e.EmployeeId)
            .ToListAsync(cancellationToken);

        return employeeIds;
    }
}
