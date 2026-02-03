using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.SubmitPlan;

public sealed class SubmitPlanHandler : IRequestHandler<SubmitPlanCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubmitPlanHandler> _logger;

    public SubmitPlanHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<SubmitPlanHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(SubmitPlanCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra người dùng đăng nhập
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
        }

        // 2. Kiểm tra quyền Department Head
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.DepartmentHead))
        {
            throw new UnauthorizedAccessException("Chỉ Department Head mới có quyền submit kế hoạch tuyển dụng.");
        }

        // 3. Lấy EnterpriseId
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        }

        // 4. Tìm plan với PlanDetails
        var plan = await _context.RecruitmentPlans
            .Include(p => p.PlanDetails)
            .Include(p => p.Campaign)
            .FirstOrDefaultAsync(p =>
                p.Id == request.PlanId &&
                p.EnterpriseId == enterpriseId.Value &&
                p.CreatedById == userId.Value &&
                !p.IsDeleted,
                cancellationToken);

        if (plan == null)
        {
            throw new Exception("Không tìm thấy kế hoạch tuyển dụng hoặc bạn không có quyền submit kế hoạch này.");
        }

        // 5. Validate status phải là Draft
        if (!PlanStatus.CanSubmit(plan.Status))
        {
            throw new Exception($"Chỉ có thể submit kế hoạch ở trạng thái 'Draft'. Trạng thái hiện tại: {PlanStatus.GetDescription(plan.Status)}");
        }

        // 6. Validate phải có ít nhất 1 PlanDetail
        var activePlanDetails = plan.PlanDetails.Where(d => !d.IsDeleted).ToList();
        if (!activePlanDetails.Any())
        {
            throw new Exception("Kế hoạch phải có ít nhất 1 chi tiết tuyển dụng trước khi submit.");
        }

        // 7. Validate TotalBudget > 0
        if (!plan.TotalBudget.HasValue || plan.TotalBudget.Value <= 0)
        {
            throw new Exception("Kế hoạch phải có ngân sách (TotalBudget > 0) trước khi submit.");
        }

        // 8. Validate Campaign status
        if (!CampaignStatus.CanSubmitPlans(plan.Campaign.Status))
        {
            throw new Exception($"Chiến dịch phải ở trạng thái 'Open' để submit kế hoạch. Trạng thái hiện tại: {plan.Campaign.Status}");
        }

        // 9. Cập nhật status Plan: Draft → Pending
        plan.Status = PlanStatus.Pending;
        plan.UpdatedAt = DateTime.UtcNow;

        // 10. Cập nhật status tất cả PlanDetails: → Pending
        foreach (var detail in activePlanDetails)
        {
            detail.Status = PlanDetailStatus.Pending;
            detail.UpdatedAt = DateTime.UtcNow;
        }

        // 11. Lưu thay đổi
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Plan {PlanId} ({PlanName}) submitted by {UserId}. Total {Count} PlanDetails.",
            plan.Id,
            plan.PlanName,
            userId.Value,
            activePlanDetails.Count);

        return true;
    }
}
