using MediatR;

namespace ERMS.Application.Features.RecruitmentCampaigns.Commands.UpdateCampaignStatus;

public sealed class UpdateCampaignStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string NewStatus { get; set; } = null!;
}
