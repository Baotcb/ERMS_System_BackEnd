using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Feedback.Queries.GetTrainerFeedbacks
{
    public class GetTrainerFeedbacksQuery : IRequest<List<TrainerFeedbackDto>> { }

    public class TrainerFeedbackDto
    {
        public string Id { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public int CourseRating { get; set; }
        public int TrainerRating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetTrainerFeedbacksHandler
        : IRequestHandler<GetTrainerFeedbacksQuery, List<TrainerFeedbackDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetTrainerFeedbacksHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<TrainerFeedbackDto>> Handle(
            GetTrainerFeedbacksQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return new List<TrainerFeedbackDto>();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
            if (user == null) return new List<TrainerFeedbackDto>();

            var trainerEmail = user.Email;

            var feedbacks = await _context.CourseFeedbacks
                .Include(f => f.Course)
                .Include(f => f.Employee).ThenInclude(e => e.User)
                .Where(f => f.Course.TrainerEmail == trainerEmail)
                .OrderByDescending(f => f.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return feedbacks.Select(f => new TrainerFeedbackDto
            {
                Id = f.Id.ToString(),
                CourseName = f.Course?.CourseName ?? "N/A",
                CourseCode = f.Course?.CourseCode ?? "",
                EmployeeName = f.IsAnonymous ? "Ẩn danh" : (f.Employee?.User?.FullName ?? f.Employee?.User?.Email ?? "N/A"),
                CourseRating = f.CourseRating,
                TrainerRating = f.TrainerRating,
                Comment = f.Comment,
                CreatedAt = f.CreatedAt
            }).ToList();
        }
    }
}
