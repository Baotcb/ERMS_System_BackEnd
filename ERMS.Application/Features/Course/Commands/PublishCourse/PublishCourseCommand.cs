using MediatR;

namespace ERMS.Application.Features.Courses.Commands.PublishCourse
{
    public sealed class PublishCourseCommand : IRequest<bool>
    {
        public string? TrainingType { get; set; }
        public string? Location { get; set; }
        public DateTime StartTime { get; set; }
        public Guid Id { get; set; }
    }
}