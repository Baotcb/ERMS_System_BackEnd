using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.ApprovePlan;

public sealed class ApprovePlanHandler : IRequestHandler<ApprovePlanCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApprovePlanHandler> _logger;

    public ApprovePlanHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ApprovePlanHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(ApprovePlanCommand request, CancellationToken cancellationToken)
    {
      
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
        }

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Director))
        {
            throw new UnauthorizedAccessException("Chỉ Director mới có quyền phê duyệt kế hoạch tuyển dụng.");
        }

      
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        }

    
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 5. Tìm plan với campaign và PlanDetails
            var plan = await _context.RecruitmentPlans
                .Include(p => p.Campaign)
                .Include(p => p.PlanDetails)
                .FirstOrDefaultAsync(p =>
                    p.Id == request.PlanId &&
                    p.EnterpriseId == enterpriseId.Value &&
                    !p.IsDeleted,
                    cancellationToken);

            if (plan == null)
            {
                throw new Exception("Không tìm thấy kế hoạch tuyển dụng.");
            }

   
            if (!PlanStatus.IsPending(plan.Status))
            {
                throw new Exception($"Chỉ có thể phê duyệt kế hoạch ở trạng thái 'Pending'. Trạng thái hiện tại: {PlanStatus.GetDescription(plan.Status)}");
            }

            
            if (!CampaignStatus.CanSubmitPlans(plan.Campaign.Status))
            {
                throw new Exception($"Chiến dịch phải ở trạng thái 'Open' để phê duyệt kế hoạch. Trạng thái hiện tại: {plan.Campaign.Status}");
            }

          
            if (plan.Campaign.TotalBudgetCeiling.HasValue && plan.TotalBudget.HasValue)
            {
               
                var usedBudget = await _context.RecruitmentPlans
                    .Where(p => p.CampaignId == plan.CampaignId 
                             && p.Status == PlanStatus.Approved 
                             && !p.IsDeleted)
                    .SumAsync(p => p.TotalBudget ?? 0, cancellationToken);

                var remainingBudget = plan.Campaign.TotalBudgetCeiling.Value - usedBudget;

                if (plan.TotalBudget.Value > remainingBudget)
                {
                    _logger.LogWarning(
                        "Director {DirectorId} approved OVER-BUDGET plan {PlanId} ({PlanName}). " +
                        "Plan budget: {PlanBudget:N0} VNĐ, Remaining: {Remaining:N0} VNĐ, " +
                        "Campaign ceiling: {Ceiling:N0} VNĐ, Used: {Used:N0} VNĐ",
                        userId.Value, plan.Id, plan.PlanName,
                        plan.TotalBudget.Value, remainingBudget,
                        plan.Campaign.TotalBudgetCeiling.Value, usedBudget);
                }
            }

            // 9. Cập nhật plan status
            plan.Status = PlanStatus.Approved;
            plan.ApprovedById = userId.Value;
            plan.ApprovedAt = DateTime.UtcNow;
            plan.UpdatedAt = DateTime.UtcNow;

            // 10. Auto-approve tất cả PlanDetails
            var activePlanDetails = plan.PlanDetails.Where(d => !d.IsDeleted).ToList();
            foreach (var detail in activePlanDetails)
            {
                detail.Status = PlanDetailStatus.Approved;
                detail.UpdatedAt = DateTime.UtcNow;
            }

            // 11. Lưu thay đổi
            await _context.SaveChangesAsync(cancellationToken);

            // 12. Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Director {DirectorId} approved plan {PlanId} ({PlanName}). Budget: {Budget} VNĐ, {Count} PlanDetails approved.",
                userId.Value,
                plan.Id,
                plan.PlanName,
                plan.TotalBudget ?? 0,
                activePlanDetails.Count);

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
