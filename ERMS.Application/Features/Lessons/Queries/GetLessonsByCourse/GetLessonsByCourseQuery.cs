using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Lessons.Queries.GetLessonsByCourse
{
    public sealed class GetLessonsByCourseQuery : IRequest<List<LessonDto>>
    {
        public Guid CourseId { get; set; }
    }

    public sealed class LessonDto
    {
        public Guid Id { get; set; }

        public string LessonTitle { get; set; } = null!;

        public string? Description { get; set; }

        public int OrderIndex { get; set; }

        public string ContentType { get; set; } = null!;

        public string? VideoUrl { get; set; }

        public int? VideoDurationMinutes { get; set; }

        public string? DocumentUrl { get; set; }

        public string? ExternalLinkUrl { get; set; }

        public bool IsPreview { get; set; }

        public int? EstimatedMinutes { get; set; }
    }
}