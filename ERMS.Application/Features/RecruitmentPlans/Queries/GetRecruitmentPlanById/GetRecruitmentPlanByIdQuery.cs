using MediatR;

namespace ERMS.Application.Features.RecruitmentPlans.Queries.GetRecruitmentPlanById;

public sealed class GetRecruitmentPlanByIdQuery : IRequest<RecruitmentPlanDetailDto>
{
    public Guid Id { get; set; }
}

public sealed class RecruitmentPlanDetailDto
{
    public Guid Id { get; set; }
    public Guid EnterpriseId { get; set; }
    public string PlanName { get; set; } = null!;
    public string PlanCode { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? TotalBudget { get; set; }
    public string Status { get; set; } = null!;
    public Guid CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
