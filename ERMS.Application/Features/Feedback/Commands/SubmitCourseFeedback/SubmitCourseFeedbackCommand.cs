using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Feedback.Commands.SubmitCourseFeedback
{
    public class SubmitCourseFeedbackCommand : IRequest<SubmitCourseFeedbackResult>
    {
        public Guid CourseId { get; set; }
        public int CourseRating { get; set; }
        public int TrainerRating { get; set; }
        public string? Comment { get; set; }
        public bool IsAnonymous { get; set; }
    }

    public class SubmitCourseFeedbackResult
    {
        public string FeedbackId { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class SubmitCourseFeedbackHandler
        : IRequestHandler<SubmitCourseFeedbackCommand, SubmitCourseFeedbackResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SubmitCourseFeedbackHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SubmitCourseFeedbackResult> Handle(
            SubmitCourseFeedbackCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
                throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy nhân viên.");

            // Check enrollment exists
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.EmployeeId == employee.Id && !e.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Bạn chưa được ghi danh vào khóa học này.");

            // Check no duplicate feedback
            var existingFeedback = await _context.CourseFeedbacks
                .AnyAsync(f => f.CourseId == request.CourseId && f.EmployeeId == employee.Id, cancellationToken);

            if (existingFeedback)
                throw new InvalidOperationException("Bạn đã gửi đánh giá cho khóa học này rồi.");

            // Validate ratings
            if (request.CourseRating < 1 || request.CourseRating > 5)
                throw new ArgumentException("Đánh giá khóa học phải từ 1 đến 5 sao.");
            if (request.TrainerRating < 1 || request.TrainerRating > 5)
                throw new ArgumentException("Đánh giá giảng viên phải từ 1 đến 5 sao.");

            var feedback = new Domain.Entities.Training.CourseFeedback
            {
                CourseId = request.CourseId,
                EmployeeId = employee.Id,
                CourseRating = request.CourseRating,
                TrainerRating = request.TrainerRating,
                Comment = request.Comment?.Trim(),
                IsAnonymous = request.IsAnonymous,
                CreatedAt = DateTime.UtcNow
            };

            _context.CourseFeedbacks.Add(feedback);
            await _context.SaveChangesAsync(cancellationToken);

            return new SubmitCourseFeedbackResult
            {
                FeedbackId = feedback.Id.ToString(),
                Message = "Đã gửi đánh giá thành công!"
            };
        }
    }
}
