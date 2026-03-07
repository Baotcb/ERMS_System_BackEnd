using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.CloseJobPosting;

public sealed class CloseJobPostingHandler : IRequestHandler<CloseJobPostingCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CloseJobPostingHandler> _logger;

    public CloseJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CloseJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(CloseJobPostingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền đóng tin tuyển dụng.");
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

        if (!JobPostingStatus.CanClose(jobPosting.Status))
        {
            throw new Exception($"Không thể đóng tin. Trạng thái hiện tại '{jobPosting.Status}' phải là 'Published'.");
        }

        jobPosting.Status = JobPostingStatus.Closed;
        jobPosting.ClosedAt = DateTime.UtcNow;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Closed JobPosting {JobPostingId}", request.Id);
        return Unit.Value;
    }
}
