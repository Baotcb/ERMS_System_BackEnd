using MediatR;

namespace ERMS.Application.Features.Courses.Commands.UpdateCourse
{
    public sealed class UpdateCourseCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public Guid? TrainingPlanId { get; set; }

        public string CourseName { get; set; } = null!;

        public string CourseCode { get; set; } = null!;

        public string? Description { get; set; }

        public string? ThumbnailUrl { get; set; }
        public string TrainerEmail { get; set; }

        public int? DurationMinutes { get; set; }

        public string? Level { get; set; }

        public bool IsMandatory { get; set; }

        public int? MaxEnrollments { get; set; }

        public DateTime? EnrollmentDeadline { get; set; }

        public string CompletionCriteria { get; set; } = "Quiz";
    }
}