using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Courses.Queries.GetCourseDetails
{
    public sealed class GetCourseDetailsQuery : IRequest<CourseDetailsDto>
    {
        public Guid Id { get; set; }
    }

    public sealed class CourseDetailsDto
    {
        public Guid Id { get; set; }

        public string CourseName { get; set; } = null!;

        public string CourseCode { get; set; } = null!;

        public string? Description { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string TrainerEmail { get; set; }
        public string? Location { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsOnline { get; set; }

        public int? DurationMinutes { get; set; }

        public string? Level { get; set; }

        public string Status { get; set; } = null!;

        public bool IsMandatory { get; set; }

        public int? MaxEnrollments { get; set; }

        public DateTime? EnrollmentDeadline { get; set; }

        public DateTime? PublishedAt { get; set; }

        public string CompletionCriteria { get; set; } = null!;

        public int LessonCount { get; set; }

        public int EnrollmentCount { get; set; }

        public bool HasFinalQuiz { get; set; }

        public Guid? FinalQuizId { get; set; }

        public List<string> Skills { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }
}