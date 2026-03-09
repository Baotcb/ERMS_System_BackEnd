using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Lessons.Queries.GetLessonsByCourse
{
    public sealed class GetLessonsByCourseHandler
        : IRequestHandler<GetLessonsByCourseQuery, List<LessonDto>>
    {
        private readonly IERMSDbContext _context;

        public GetLessonsByCourseHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<List<LessonDto>> Handle(GetLessonsByCourseQuery request, CancellationToken cancellationToken)
        {
            var lessons = await _context.Lessons
                .Where(l => l.CourseId == request.CourseId && !l.IsDeleted)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonDto
                {
                    Id = l.Id,
                    LessonTitle = l.LessonTitle,
                    Description = l.Description,
                    OrderIndex = l.OrderIndex,
                    ContentType = l.ContentType,
                    VideoUrl = l.VideoUrl,
                    VideoDurationMinutes = l.VideoDurationMinutes,
                    DocumentUrl = l.DocumentUrl,
                    ExternalLinkUrl = l.ExternalLinkUrl,
                    IsPreview = l.IsPreview,
                    EstimatedMinutes = l.EstimatedMinutes
                })
                .ToListAsync(cancellationToken);

            return lessons;
        }
    }
}