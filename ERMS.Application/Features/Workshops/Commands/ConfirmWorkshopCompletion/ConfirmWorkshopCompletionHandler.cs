using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Workshops.Commands.ConfirmWorkshopCompletion
{
    public sealed class ConfirmWorkshopCompletionHandler
        : IRequestHandler<ConfirmWorkshopCompletionCommand, Guid>
    {
        private readonly IERMSDbContext _context;

        public ConfirmWorkshopCompletionHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(
            ConfirmWorkshopCompletionCommand request,
            CancellationToken cancellationToken)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == request.CourseId && !c.IsDeleted, cancellationToken)
                ?? throw new Exception("Không tìm thấy khóa học.");

            if (course.IsOnline)
            {
                throw new Exception("Chỉ khóa học offline (workshop) mới cần xác nhận hoàn thành.");
            }

            if (course.Status != "Published")
            {
                throw new Exception("Khóa học chưa được xuất bản.");
            }

            // Check if already confirmed
            var existing = await _context.WorkshopConfirmations
                .AnyAsync(w => w.CourseId == request.CourseId, cancellationToken);

            if (existing)
            {
                throw new Exception("Workshop này đã được xác nhận hoàn thành trước đó.");
            }

            // Create confirmation
            var confirmation = new WorkshopConfirmation
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                ConfirmedByUserId = Guid.Empty, // Will be set from auth context by controller
                EvidencePhotoUrls = JsonSerializer.Serialize(request.EvidencePhotoUrls),
                Notes = request.Notes,
                ConfirmedAt = DateTime.UtcNow
            };

            _context.WorkshopConfirmations.Add(confirmation);

            // Mark all enrolled trainees as completed
            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == request.CourseId && !e.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var enrollment in enrollments)
            {
                enrollment.Status = "WorkshopCompleted";
                enrollment.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return confirmation.Id;
        }
    }
}
