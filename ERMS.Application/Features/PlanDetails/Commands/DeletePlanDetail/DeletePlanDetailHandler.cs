using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.PlanDetails.Commands.DeletePlanDetail;

public sealed class DeletePlanDetailHandler : IRequestHandler<DeletePlanDetailCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeletePlanDetailHandler> _logger;

    public DeletePlanDetailHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeletePlanDetailHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeletePlanDetailCommand request, CancellationToken cancellationToken)
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
            throw new UnauthorizedAccessException("Chỉ Department Head mới có quyền xóa chi tiết kế hoạch tuyển dụng.");
        }

        // 3. Lấy EnterpriseId
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        }

        // 4. Tìm PlanDetail với Plan và Campaign
        var planDetail = await _context.PlanDetails
            .Include(d => d.RecruitmentPlan)
                .ThenInclude(p => p.Campaign)
            .FirstOrDefaultAsync(d =>
                d.Id == request.Id &&
                d.RecruitmentPlan.EnterpriseId == enterpriseId.Value &&
                d.RecruitmentPlan.CreatedById == userId.Value &&
                !d.IsDeleted,
                cancellationToken);

        if (planDetail == null)
        {
            throw new Exception("Không tìm thấy chi tiết kế hoạch hoặc bạn không có quyền xóa.");
        }

        // 5. Validate Plan status (Draft OR Rejected)
        if (!PlanStatus.CanEditPlanDetails(planDetail.RecruitmentPlan.Status))
        {
            throw new Exception($"Chỉ có thể xóa chi tiết khi kế hoạch ở trạng thái 'Draft' hoặc 'Rejected'. Trạng thái hiện tại: {PlanStatus.GetDescription(planDetail.RecruitmentPlan.Status)}");
        }

        // 6. Validate Campaign status
        if (!CampaignStatus.CanSubmitPlans(planDetail.RecruitmentPlan.Campaign.Status))
        {
            throw new Exception($"Chiến dịch phải ở trạng thái 'Open' để xóa chi tiết kế hoạch. Trạng thái hiện tại: {planDetail.RecruitmentPlan.Campaign.Status}");
        }

        // 7. Soft delete
        planDetail.IsDeleted = true;
        planDetail.DeletedAt = DateTime.UtcNow;
        planDetail.UpdatedAt = DateTime.UtcNow;

        // 8. Recalculate TotalBudget của Plan (không tính detail bị xóa)
        var totalBudget = await _context.PlanDetails
            .Where(d => d.RecruitmentPlanId == planDetail.RecruitmentPlanId
                     && d.Id != planDetail.Id // Không tính detail vừa xóa
                     && d.Status != PlanDetailStatus.Rejected
                     && !d.IsDeleted)
            .SumAsync(d => d.Quantity * (d.SalaryRangeMax ?? 0), cancellationToken);

        planDetail.RecruitmentPlan.TotalBudget = totalBudget;
        planDetail.RecruitmentPlan.UpdatedAt = DateTime.UtcNow;

        // 9. Lưu thay đổi
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted PlanDetail {DetailId}. New TotalBudget of Plan {PlanId}: {Budget} VNĐ",
            planDetail.Id,
            planDetail.RecruitmentPlanId,
            totalBudget);

        return true;
    }
}
