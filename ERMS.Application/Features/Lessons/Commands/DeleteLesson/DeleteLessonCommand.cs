using MediatR;

namespace ERMS.Application.Features.Lessons.Commands.DeleteLesson
{
    public sealed class DeleteLessonCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
    }
}