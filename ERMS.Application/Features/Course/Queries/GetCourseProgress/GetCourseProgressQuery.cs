using MediatR;

namespace ERMS.Application.Features.Courses.Queries.GetCourseProgress;

public class GetCourseProgressQuery : IRequest<CourseProgressDto>
{
    public Guid CourseId { get; set; }
}