using MediatR;
using System;

namespace ERMS.Application.Features.Lessons.Commands.UpdateLessonProgress
{
    public sealed class UpdateLessonProgressCommand : IRequest<bool>
    {
        public Guid LessonId { get; set; }

        public int WatchPercentage { get; set; }

        public int? LastPosition { get; set; }

        public int TimeSpentMinutes { get; set; }
    }
}