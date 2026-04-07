using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.PublishJobPosting;

public sealed class PublishJobPostingHandler : IRequestHandler<PublishJobPostingCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PublishJobPostingHandler> _logger;

    public PublishJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<PublishJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(PublishJobPostingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền đăng tin tuyển dụng.");
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

        // Validate current status is Draft
        if (!JobPostingStatus.CanPublish(jobPosting.Status))
        {
            throw new Exception($"Không thể đăng tin. Trạng thái hiện tại '{jobPosting.Status}' phải là 'Draft'.");
        }

        jobPosting.Status = JobPostingStatus.Published;
        jobPosting.PublishedAt = DateTime.UtcNow;
        jobPosting.PublishedById = userId;
        jobPosting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã xuất bản bài tuyển dụng {JobPostingId}", request.Id);
        return Unit.Value;
    }
}
