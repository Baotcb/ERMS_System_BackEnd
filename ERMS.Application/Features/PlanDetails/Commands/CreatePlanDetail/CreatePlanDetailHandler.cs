using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.PlanDetails.Commands.CreatePlanDetail;

public sealed class CreatePlanDetailHandler : IRequestHandler<CreatePlanDetailCommand, Guid>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatePlanDetailHandler> _logger;

    public CreatePlanDetailHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreatePlanDetailHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreatePlanDetailCommand request, CancellationToken cancellationToken)
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
            throw new UnauthorizedAccessException("Chỉ Department Head mới có quyền tạo chi tiết kế hoạch tuyển dụng.");
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

        // 5. Tìm Plan với Campaign
        var plan = await _context.RecruitmentPlans
            .Include(p => p.Campaign)
            .FirstOrDefaultAsync(p =>
                p.Id == request.RecruitmentPlanId &&
                p.EnterpriseId == enterpriseId.Value &&
                p.CreatedById == userId.Value &&
                !p.IsDeleted,
                cancellationToken);

        if (plan == null)
        {
            throw new Exception("Không tìm thấy kế hoạch tuyển dụng hoặc bạn không có quyền chỉnh sửa kế hoạch này.");
        }

        // 6. Validate Plan status (Draft OR Rejected)
        if (!PlanStatus.CanEditPlanDetails(plan.Status))
        {
            throw new Exception($"Chỉ có thể thêm chi tiết khi kế hoạch ở trạng thái 'Draft' hoặc 'Rejected'. Trạng thái hiện tại: {PlanStatus.GetDescription(plan.Status)}");
        }

        // 7. Validate Campaign status
        if (!CampaignStatus.CanSubmitPlans(plan.Campaign.Status))
        {
            throw new Exception($"Chiến dịch phải ở trạng thái 'Open' để thêm chi tiết kế hoạch. Trạng thái hiện tại: {plan.Campaign.Status}");
        }

        // 8. Tạo PlanDetail mới
        var planDetail = new PlanDetail
        {
            Id = Guid.CreateVersion7(),
            RecruitmentPlanId = request.RecruitmentPlanId,
            RequestedById = userId.Value,
            PositionTitle = request.PositionTitle.Trim(),
            Quantity = request.Quantity,
            Priority = request.Priority,
            Justification = request.Justification?.Trim(),
            RequiredSkills = request.RequiredSkills?.Trim(),
            MinExperience = request.MinExperience,
            MaxExperience = request.MaxExperience,
            EducationLevel = request.EducationLevel?.Trim(),
            SalaryRangeMin = request.SalaryRangeMin,
            SalaryRangeMax = request.SalaryRangeMax,
            ExpectedStartDate = request.ExpectedStartDate,
            Status = PlanDetailStatus.Pending,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PlanDetails.Add(planDetail);

        // 9. Recalculate TotalBudget của Plan
        var totalBudget = await _context.PlanDetails
            .Where(d => d.RecruitmentPlanId == request.RecruitmentPlanId
                     && d.Status != PlanDetailStatus.Rejected
                     && !d.IsDeleted)
            .SumAsync(d => d.Quantity * (d.SalaryRangeMax ?? 0), cancellationToken);

        // Cộng thêm detail vừa tạo
        totalBudget += request.Quantity * request.SalaryRangeMax;

        plan.TotalBudget = totalBudget;
        plan.UpdatedAt = DateTime.UtcNow;

        // 10. Lưu thay đổi
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created PlanDetail {DetailId} for Plan {PlanId}. New TotalBudget: {Budget} VNĐ",
            planDetail.Id,
            plan.Id,
            totalBudget);

        return planDetail.Id;
    }
}
