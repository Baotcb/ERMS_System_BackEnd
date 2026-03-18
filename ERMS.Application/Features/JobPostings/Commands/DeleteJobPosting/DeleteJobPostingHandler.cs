using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using ERMS.Domain.Constants.Recruitment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting;

public sealed class DeleteJobPostingHandler : IRequestHandler<DeleteJobPostingCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteJobPostingHandler> _logger;

    public DeleteJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteJobPostingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền xóa tin tuyển dụng.");
        }

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(jp =>
                jp.Id == request.Id
                && jp.EnterpriseId == enterpriseId
                && !jp.IsDeleted,
                cancellationToken)
            ?? throw new Exception($"Không tìm thấy tin tuyển dụng với ID {request.Id}.");

        // Check if job posting is published and has applications
        if (JobPostingStatus.IsPublished(jobPosting.Status))
        {
            var hasApplications = await _context.Applications
                .AnyAsync(a => a.JobPostingId == request.Id && !a.IsDeleted, cancellationToken);

            if (hasApplications)
            {
                throw new InvalidOperationException("Không thể xóa tin tuyển dụng đã đăng có ứng viên nộp hồ sơ. Vui lòng đóng tin tuyển dụng thay vì xóa.");
            }
        }

        jobPosting.IsDeleted = true;
        jobPosting.DeletedAt = DateTime.UtcNow;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted JobPosting {JobPostingId}", request.Id);
        return Unit.Value;
    }
}
