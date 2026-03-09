using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Courses.Queries.GetAllCourses
{
    public sealed class GetAllCoursesHandler : IRequestHandler<GetAllCourseQuery, GetAllCoursesResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAllCoursesHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetAllCoursesResult> Handle(GetAllCourseQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
            {
                throw new System.UnauthorizedAccessException("User does not belong to any enterprise");
            }

            var query = _context.Courses
                .Where(c => c.EnterpriseId == enterpriseId.Value && !c.IsDeleted)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();

                query = query.Where(c =>
                    c.CourseName.ToLower().Contains(search) ||
                    c.CourseCode.ToLower().Contains(search));
            }

            // Status filter
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(c => c.Status == request.Status);
            }

            // Mandatory filter
            if (request.IsMandatory.HasValue)
            {
                query = query.Where(c => c.IsMandatory == request.IsMandatory.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CourseDto
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    CourseCode = c.CourseCode,
                    Description = c.Description,
                    ThumbnailUrl = c.ThumbnailUrl,
                    TrainerId = c.TrainerId,
                    TrainerName = c.Trainer.User.FullName,
                    DurationMinutes = c.DurationMinutes,
                    Level = c.Level,
                    Status = c.Status,
                    IsMandatory = c.IsMandatory,
                    MaxEnrollments = c.MaxEnrollments,
                    EnrollmentDeadline = c.EnrollmentDeadline,
                    PublishedAt = c.PublishedAt,
                    LessonCount = c.Lessons.Count(l => !l.IsDeleted),
                    EnrollmentCount = c.Enrollments.Count(e => !e.IsDeleted),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new GetAllCoursesResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}