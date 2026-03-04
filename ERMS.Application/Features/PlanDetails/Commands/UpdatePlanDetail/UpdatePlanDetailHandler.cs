using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.PlanDetails.Commands.UpdatePlanDetail;

public sealed class UpdatePlanDetailHandler : IRequestHandler<UpdatePlanDetailCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdatePlanDetailHandler> _logger;

    public UpdatePlanDetailHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdatePlanDetailHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdatePlanDetailCommand request, CancellationToken cancellationToken)
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
            throw new UnauthorizedAccessException("Chỉ Department Head mới có quyền cập nhật chi tiết kế hoạch tuyển dụng.");
        }

        // 3. Lấy EnterpriseId
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        }

        // 4. Validate input
        if (request.Quantity <= 0)
        {
            throw new Exception("Số lượng (Quantity) phải lớn hơn 0.");
        }

        if (request.SalaryRangeMin.HasValue && request.SalaryRangeMax < request.SalaryRangeMin.Value)
        {
            throw new Exception("SalaryRangeMax phải lớn hơn hoặc bằng SalaryRangeMin.");
        }

        // 5. Tìm PlanDetail với Plan và Campaign
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
            throw new Exception("Không tìm thấy chi tiết kế hoạch hoặc bạn không có quyền chỉnh sửa.");
        }

        // 6. Validate Plan status (Draft OR Rejected)
        if (!PlanStatus.CanEditPlanDetails(planDetail.RecruitmentPlan.Status))
        {
            throw new Exception($"Chỉ có thể sửa chi tiết khi kế hoạch ở trạng thái 'Draft' hoặc 'Rejected'. Trạng thái hiện tại: {PlanStatus.GetDescription(planDetail.RecruitmentPlan.Status)}");
        }

        // 7. Validate Campaign status
        if (!CampaignStatus.CanSubmitPlans(planDetail.RecruitmentPlan.Campaign.Status))
        {
            throw new Exception($"Chiến dịch phải ở trạng thái 'Open' để sửa chi tiết kế hoạch. Trạng thái hiện tại: {planDetail.RecruitmentPlan.Campaign.Status}");
        }

        // 8. Cập nhật thông tin
        planDetail.PositionTitle = request.PositionTitle.Trim();
        planDetail.Quantity = request.Quantity;
        planDetail.Priority = request.Priority;
        planDetail.Justification = request.Justification?.Trim();
        planDetail.RequiredSkills = request.RequiredSkills?.Trim();
        planDetail.MinExperience = request.MinExperience;
        planDetail.MaxExperience = request.MaxExperience;
        planDetail.EducationLevel = request.EducationLevel?.Trim();
        planDetail.SalaryRangeMin = request.SalaryRangeMin;
        planDetail.SalaryRangeMax = request.SalaryRangeMax;
        planDetail.ExpectedStartDate = request.ExpectedStartDate;
        planDetail.UpdatedAt = DateTime.UtcNow;

        // 9. Recalculate TotalBudget của Plan
        var totalBudget = await _context.PlanDetails
            .Where(d => d.RecruitmentPlanId == planDetail.RecruitmentPlanId
                     && d.Status != PlanDetailStatus.Rejected
                     && !d.IsDeleted)
            .SumAsync(d => d.Quantity * (d.SalaryRangeMax ?? 0), cancellationToken);

        planDetail.RecruitmentPlan.TotalBudget = totalBudget;
        planDetail.RecruitmentPlan.UpdatedAt = DateTime.UtcNow;

        // 10. Lưu thay đổi
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated PlanDetail {DetailId}. New TotalBudget of Plan {PlanId}: {Budget} VNĐ",
            planDetail.Id,
            planDetail.RecruitmentPlanId,
            totalBudget);

        return true;
    }
}
