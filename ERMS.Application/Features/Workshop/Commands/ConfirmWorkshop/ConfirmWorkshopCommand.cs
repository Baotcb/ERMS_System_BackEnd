using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ERMS.Application.Features.Workshop.Commands.ConfirmWorkshop
{
    public class ConfirmWorkshopCommand : IRequest<ConfirmWorkshopResult>
    {
        public Guid CourseId { get; set; }
        public List<string> EvidencePhotoUrls { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class ConfirmWorkshopResult
    {
        public string Message { get; set; } = "";
        public string ConfirmationId { get; set; } = "";
    }

    public class ConfirmWorkshopHandler : IRequestHandler<ConfirmWorkshopCommand, ConfirmWorkshopResult>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ConfirmWorkshopHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ConfirmWorkshopResult> Handle(ConfirmWorkshopCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) throw new UnauthorizedAccessException();

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy khóa học.");

            if (course.IsOnline)
                throw new InvalidOperationException("Chỉ có thể xác nhận workshop cho khóa đào tạo offline.");

            // Check if already confirmed
            var existing = await _context.WorkshopConfirmations
                .AnyAsync(w => w.CourseId == request.CourseId, cancellationToken);
            if (existing)
                throw new InvalidOperationException("Workshop này đã được xác nhận trước đó.");

            if (request.EvidencePhotoUrls == null || request.EvidencePhotoUrls.Count == 0)
                throw new ArgumentException("Cần ít nhất 1 ảnh minh chứng để xác nhận workshop.");

            var confirmation = new Domain.Entities.Training.WorkshopConfirmation
            {
                CourseId = request.CourseId,
                ConfirmedByUserId = userId.Value,
                EvidencePhotoUrls = JsonSerializer.Serialize(request.EvidencePhotoUrls),
                Notes = request.Notes?.Trim(),
                ConfirmedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false

            };

            _context.WorkshopConfirmations.Add(confirmation);
            await _context.SaveChangesAsync(cancellationToken);

            return new ConfirmWorkshopResult
            {
                Message = "Đã xác nhận hoàn thành workshop thành công!",
                ConfirmationId = confirmation.Id.ToString()
            };
        }
    }
}
