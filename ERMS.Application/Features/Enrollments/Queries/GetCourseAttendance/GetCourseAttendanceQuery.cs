using MediatR;
using System;

namespace ERMS.Application.Features.Enrollments.Queries.GetCourseAttendance
{
    public sealed class GetCourseAttendanceQuery : IRequest<List<CourseAttendanceDto>>
    {
        public Guid CourseId { get; set; }
    }
}