using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Enrollments.Commands.AssignEmployeesToCourse
{
    public sealed class AssignEmployeesToCourseCommand : IRequest<AssignEmployeesToCourseResult>
    {
        public Guid CourseId { get; set; }
        public string MeetUrl { get; set; } = string.Empty;

        public List<Guid> EmployeeIds { get; set; } = new();
    }

    public class AssignEmployeesRequest
    {
        public string? MeetUrl { get; set; } = string.Empty;

        public List<Guid> EmployeeIds { get; set; } = new();
    }

    public sealed class AssignEmployeesToCourseResult
    {
        public int TotalAssigned { get; set; }

        public List<Guid> AssignedEmployeeIds { get; set; } = new();

        public List<Guid> SkippedEmployeeIds { get; set; } = new();
    }
}