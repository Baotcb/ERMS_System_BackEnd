using MediatR;

namespace ERMS.Application.Features.Courses.Commands.PublishCourse
{
    public sealed class PublishCourseCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}