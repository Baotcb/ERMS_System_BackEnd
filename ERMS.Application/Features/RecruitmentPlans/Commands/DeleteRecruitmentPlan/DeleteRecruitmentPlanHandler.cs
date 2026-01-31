using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.DeleteRecruitmentPlan;

public sealed class DeleteRecruitmentPlanHandler : IRequestHandler<DeleteRecruitmentPlanCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteRecruitmentPlanHandler> _logger;

    public DeleteRecruitmentPlanHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteRecruitmentPlanHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteRecruitmentPlanCommand request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");
        }

        // Find recruitment plan
        var recruitmentPlan = await _context.RecruitmentPlans
            .FirstOrDefaultAsync(rp => rp.Id == request.Id
                                    && rp.EnterpriseId == enterpriseId.Value
                                    && !rp.IsDeleted, cancellationToken);

        if (recruitmentPlan == null)
        {
            throw new Exception("Không tìm thấy kế hoạch tuyển dụng");
        }

        // Soft delete
        recruitmentPlan.IsDeleted = true;
        recruitmentPlan.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted recruitment plan {PlanName} (ID: {PlanId})",
            recruitmentPlan.PlanName,
            recruitmentPlan.Id);

        return Unit.Value;
    }
}
