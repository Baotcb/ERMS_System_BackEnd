using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Courses.Queries.GetCourseDetails
{
    public sealed class GetCourseDetailsHandler : IRequestHandler<GetCourseDetailsQuery, CourseDetailsDto>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCourseDetailsHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseDetailsDto> Handle(GetCourseDetailsQuery request, CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

            var course = await _context.Courses
                .Where(c => c.Id == request.Id &&
                            c.EnterpriseId == enterpriseId.Value &&
                            !c.IsDeleted)
                .Select(c => new CourseDetailsDto
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    CourseCode = c.CourseCode,
                    Description = c.Description,
                    ThumbnailUrl = c.ThumbnailUrl,
                    TrainerEmail = c.TrainerEmail,
                    DurationMinutes = c.DurationMinutes,
                    StartTime = c.StartTime,
                    IsOnline = c.IsOnline,
                    Location = c.Location,
                    Level = c.Level,
                    Status = c.Status,
                    IsMandatory = c.IsMandatory,
                    MaxEnrollments = c.MaxEnrollments,
                    EnrollmentDeadline = c.EnrollmentDeadline,
                    PublishedAt = c.PublishedAt,
                    CompletionCriteria = c.CompletionCriteria,

                    LessonCount = c.Lessons.Count(l => !l.IsDeleted),
                    EnrollmentCount = c.Enrollments.Count(e => !e.IsDeleted),

                    Skills = c.CourseSkills
                        .Select(cs => cs.Skill.SkillName)
                        .ToList(),

                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (course == null)
                throw new KeyNotFoundException("Không tìm thấy khóa học.");

            return course;
        }
    }
}