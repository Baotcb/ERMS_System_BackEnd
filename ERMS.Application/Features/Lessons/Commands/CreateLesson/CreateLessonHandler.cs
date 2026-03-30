using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Lessons.Commands.CreateLesson
{
    public sealed class CreateLessonHandler : IRequestHandler<CreateLessonCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateLessonHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == request.CourseId && !c.IsDeleted, cancellationToken);

            if (course == null)
                throw new KeyNotFoundException("Không tìm thấy khóa học");

            var enterpriseId = _currentUserService.GetEnterpriseIdAsync;
            var role = _currentUserService.Roles;

            if (!role.Contains("HR") && !enterpriseId.Equals(course.EnterpriseId))
            {
               throw new UnauthorizedAccessException("Bạn không có quyền thêm bài học vào khóa học này");

            }


            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                LessonTitle = request.LessonTitle,
                Description = request.Description,
                OrderIndex = request.OrderIndex,
                ContentType = request.ContentType,
                VideoUrl = request.VideoUrl,
                VideoDurationMinutes = request.VideoDurationMinutes,
                DocumentUrl = request.DocumentUrl,
                ExternalLinkUrl = request.ExternalLinkUrl,
                Content = request.Content,
                IsPreview = request.IsPreview,
                EstimatedMinutes = request.EstimatedMinutes
            };

            _context.Lessons.Add(lesson);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return lesson.Id;
        }
    }
}