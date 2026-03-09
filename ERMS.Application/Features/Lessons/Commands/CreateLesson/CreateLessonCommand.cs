using MediatR;
using System;

namespace ERMS.Application.Features.Lessons.Commands.CreateLesson
{
    public sealed class CreateLessonCommand : IRequest<Guid>
    {
        public Guid CourseId { get; set; }

        public string LessonTitle { get; set; } = null!;

        public string? Description { get; set; }

        public int OrderIndex { get; set; }

        public string ContentType { get; set; } = "Video";

        public string? VideoUrl { get; set; }

        public int? VideoDurationMinutes { get; set; }

        public string? DocumentUrl { get; set; }

        public string? ExternalLinkUrl { get; set; }

        public string? Content { get; set; }

        public bool IsPreview { get; set; }

        public int? EstimatedMinutes { get; set; }
    }
}