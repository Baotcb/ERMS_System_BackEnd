using ERMS.Application.Interface;
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
            Id = Guid.NewGuid(),
            EnterpriseId = enterpriseId.Value,
            PlanName = request.PlanName,
            PlanCode = request.PlanCode,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalBudget = request.TotalBudget,
            Status = "Pending",
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
