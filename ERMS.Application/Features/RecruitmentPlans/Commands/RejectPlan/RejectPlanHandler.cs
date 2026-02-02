using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.RejectPlan;

public sealed class RejectPlanHandler : IRequestHandler<RejectPlanCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RejectPlanHandler> _logger;

    public RejectPlanHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<RejectPlanHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(RejectPlanCommand request, CancellationToken cancellationToken)
    {
     
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
        }

     
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Director))
        {
            throw new UnauthorizedAccessException("Chỉ Director mới có quyền từ chối kế hoạch tuyển dụng.");
        }

     
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        }

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            throw new Exception("Lý do từ chối là bắt buộc.");
        }

        if (request.RejectionReason.Length > 1000)
        {
            throw new Exception("Lý do từ chối không được vượt quá 1000 ký tự.");
        }

      
        var plan = await _context.RecruitmentPlans
            .Include(p => p.Campaign)
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
            throw new Exception($"Chỉ có thể từ chối kế hoạch ở trạng thái 'Pending'. Trạng thái hiện tại: {PlanStatus.GetDescription(plan.Status)}");
        }

    
        plan.Status = PlanStatus.Rejected;
        plan.RejectionReason = request.RejectionReason.Trim();
        plan.RejectedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

      
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Director {DirectorId} rejected plan {PlanId} ({PlanName}). Reason: {Reason}",
            userId.Value,
            plan.Id,
            plan.PlanName,
            request.RejectionReason);

        return true;
    }
}
