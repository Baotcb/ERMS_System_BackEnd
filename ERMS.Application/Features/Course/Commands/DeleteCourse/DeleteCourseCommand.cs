using MediatR;

namespace ERMS.Application.Features.Courses.Commands.DeleteCourse
{
    public sealed class DeleteCourseCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
    }
}