using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.RecruitmentCampaigns.Commands.UpdateCampaignStatus;

public sealed class UpdateCampaignStatusHandler : IRequestHandler<UpdateCampaignStatusCommand, bool>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCampaignStatusHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateCampaignStatusCommand request, CancellationToken cancellationToken)
    {
       
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
        }

       
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền thay đổi trạng thái chiến dịch.");
        }

       
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
        if (enterpriseId == null)
        {
            throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        }

    
        if (!CampaignStatus.IsValid(request.NewStatus))
        {
            throw new Exception($"Trạng thái '{request.NewStatus}' không hợp lệ. Các trạng thái hợp lệ: {string.Join(", ", CampaignStatus.ValidStatuses)}");
        }

        var campaign = await _context.RecruitmentCampaigns
            .FirstOrDefaultAsync(c =>
                c.Id == request.Id &&
                c.EnterpriseId == enterpriseId.Value &&
                !c.IsDeleted,
                cancellationToken);

        if (campaign == null)
        {
            throw new Exception("Không tìm thấy chiến dịch tuyển dụng.");
        }

      
        if (!CampaignStatus.CanTransitionTo(campaign.Status, request.NewStatus))
        {
            throw new Exception($"Không thể chuyển từ trạng thái '{campaign.Status}' sang '{request.NewStatus}'.");
        }


        campaign.Status = request.NewStatus;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
