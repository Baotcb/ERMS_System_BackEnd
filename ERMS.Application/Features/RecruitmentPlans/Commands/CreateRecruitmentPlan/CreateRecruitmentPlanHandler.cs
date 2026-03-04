using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Entities.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.RecruitmentPlans.Commands.CreateRecruitmentPlan;

public sealed class CreateRecruitmentPlanHandler : IRequestHandler<CreateRecruitmentPlanCommand, Guid>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateRecruitmentPlanHandler> _logger;

    public CreateRecruitmentPlanHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateRecruitmentPlanHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateRecruitmentPlanCommand request, CancellationToken cancellationToken)
    {
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào");
        }

        // Validate enterprise exists
        var enterpriseExists = await _context.Enterprises
            .AnyAsync(e => e.Id == enterpriseId && !e.IsDeleted, cancellationToken);

        if (!enterpriseExists)
        {
            throw new Exception("Doanh nghiệp không tồn tại");
        }

        // Check duplicate plan code within enterprise
        var codeExists = await _context.RecruitmentPlans
            .AnyAsync(rp => rp.EnterpriseId == enterpriseId.Value
                         && rp.PlanCode == request.PlanCode
                         && !rp.IsDeleted, cancellationToken);

        if (codeExists)
        {
            throw new Exception("Mã kế hoạch tuyển dụng đã tồn tại trong doanh nghiệp");
        }

        // Validate CampaignId exists and belongs to enterprise
        var campaign = await _context.RecruitmentCampaigns
            .FirstOrDefaultAsync(c =>
                c.Id == request.CampaignId &&
                c.EnterpriseId == enterpriseId.Value &&
                !c.IsDeleted,
                cancellationToken);

        if (campaign == null)
        {
            throw new Exception("Chiến dịch tuyển dụng không tồn tại hoặc không thuộc doanh nghiệp của bạn.");
        }

        // Validate Campaign status must be Open
        if (!CampaignStatus.CanSubmitPlans(campaign.Status))
        {
            throw new Exception($"Chiến dịch phải ở trạng thái 'Open' để tạo kế hoạch. Trạng thái hiện tại: {campaign.Status}");
        }

        // Validate dates
        if (request.EndDate <= request.StartDate)
        {
            throw new Exception("Ngày kết thúc phải sau ngày bắt đầu");
        }

        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng");
        }

        var recruitmentPlan = new RecruitmentPlan
        {
            Id = Guid.CreateVersion7(),
            EnterpriseId = enterpriseId.Value,
            CampaignId = request.CampaignId,
            DepartmentId = request.DepartmentId,
            PlanName = request.PlanName,
            PlanCode = request.PlanCode,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalBudget = request.TotalBudget,
            Status = PlanStatus.Draft,
            CreatedById = userId.Value,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RecruitmentPlans.Add(recruitmentPlan);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created recruitment plan {PlanName} (ID: {PlanId}) for enterprise {EnterpriseId}",
            recruitmentPlan.PlanName,
            recruitmentPlan.Id,
            recruitmentPlan.EnterpriseId);

        return recruitmentPlan.Id;
    }
}
