using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.UpdateRecruitmentPlan;

public sealed class UpdateRecruitmentPlanHandler : IRequestHandler<UpdateRecruitmentPlanCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateRecruitmentPlanHandler> _logger;

    public UpdateRecruitmentPlanHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateRecruitmentPlanHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateRecruitmentPlanCommand request, CancellationToken cancellationToken)
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

        // Check duplicate plan code (exclude self)
        var codeExists = await _context.RecruitmentPlans
            .AnyAsync(rp => rp.Id != request.Id
                         && rp.EnterpriseId == enterpriseId.Value
                         && rp.PlanCode == request.PlanCode
                         && !rp.IsDeleted, cancellationToken);

        if (codeExists)
        {
            throw new Exception("Mã kế hoạch tuyển dụng đã tồn tại trong doanh nghiệp");
        }

        // Validate dates
        if (request.EndDate <= request.StartDate)
        {
            throw new Exception("Ngày kết thúc phải sau ngày bắt đầu");
        }

        // Update fields
        recruitmentPlan.PlanName = request.PlanName;
        recruitmentPlan.PlanCode = request.PlanCode;
        recruitmentPlan.Description = request.Description;
        recruitmentPlan.StartDate = request.StartDate;
        recruitmentPlan.EndDate = request.EndDate;
        recruitmentPlan.TotalBudget = request.TotalBudget;
        recruitmentPlan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated recruitment plan {PlanName} (ID: {PlanId})",
            recruitmentPlan.PlanName,
            recruitmentPlan.Id);

        return Unit.Value;
    }
}
