using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.RecruitmentCampaigns.Commands.CreateRecruitmentCampaign;

public sealed class CreateRecruitmentCampaignHandler : IRequestHandler<CreateRecruitmentCampaignCommand, Guid>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateRecruitmentCampaignHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateRecruitmentCampaignCommand request, CancellationToken cancellationToken)
    {
      
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
        }

 
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền tạo chiến dịch tuyển dụng.");
        }

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        }

        if (request.SubmissionEndDate <= request.SubmissionStartDate)
        {
            throw new Exception("Ngày kết thúc phải sau ngày bắt đầu.");
        }

        if (request.TargetHireStartDate.HasValue && request.TargetHireEndDate.HasValue &&
            request.TargetHireEndDate.Value <= request.TargetHireStartDate.Value)
        {
            throw new Exception("Ngày kết thúc tuyển dụng phải sau ngày bắt đầu tuyển dụng.");
        }

       
        if (request.FiscalYear < 2020)
        {
            throw new Exception("Năm tài chính phải từ 2020 trở đi.");
        }

        if (request.FiscalQuarter.HasValue && (request.FiscalQuarter < 1 || request.FiscalQuarter > 4))
        {
            throw new Exception("Quý tài chính phải từ 1 đến 4.");
        }

        var existingCampaign = await _context.RecruitmentCampaigns
            .FirstOrDefaultAsync(c =>
                c.EnterpriseId == enterpriseId.Value &&
                c.CampaignCode.ToLower() == request.CampaignCode.Trim().ToLower() &&
                !c.IsDeleted,
                cancellationToken);

        if (existingCampaign != null)
        {
            throw new Exception($"Mã chiến dịch '{request.CampaignCode}' đã tồn tại trong doanh nghiệp.");
        }

        
        var campaign = new RecruitmentCampaign
        {
            Id = Guid.NewGuid(),
            EnterpriseId = enterpriseId.Value,
            CampaignName = request.CampaignName.Trim(),
            CampaignCode = request.CampaignCode.Trim(),
            Description = request.Description?.Trim(),
            FiscalYear = request.FiscalYear,
            FiscalQuarter = request.FiscalQuarter,
            SubmissionStartDate = request.SubmissionStartDate,
            SubmissionEndDate = request.SubmissionEndDate,
            TargetHireStartDate = request.TargetHireStartDate,
            TargetHireEndDate = request.TargetHireEndDate,
            TotalBudgetCeiling = request.TotalBudgetCeiling,
            MaxTotalPositions = request.MaxTotalPositions,
            Status = CampaignStatus.Draft,
            CreatedById = userId.Value,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RecruitmentCampaigns.Add(campaign);
        await _context.SaveChangesAsync(cancellationToken);

        return campaign.Id;
    }
}
