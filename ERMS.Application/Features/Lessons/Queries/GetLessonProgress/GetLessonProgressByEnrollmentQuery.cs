using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Lessons.Queries.GetLessonProgress
{
    public sealed class GetLessonProgressByEnrollmentQuery : IRequest<List<LessonProgressDto>>
    {
        public Guid EnrollmentId { get; set; }
    }

    public sealed class LessonProgressDto
    {
        public Guid LessonId { get; set; }

        public string LessonTitle { get; set; } = null!;

        public int OrderIndex { get; set; }

        public int WatchPercentage { get; set; }

        public int? LastPosition { get; set; }

        public int TimeSpentMinutes { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}