using MediatR;

namespace ERMS.Application.Features.PlanDetails.Queries.GetAllPlanDetails;

public sealed class GetAllPlanDetailsQuery : IRequest<List<PlanDetailDto>>
{
    public Guid RecruitmentPlanId { get; set; }
}

public sealed class PlanDetailDto
{
    public Guid Id { get; set; }
    public Guid RecruitmentPlanId { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string PositionTitle { get; set; } = null!;
    public int Quantity { get; set; }
    public string Priority { get; set; } = null!;
    public string? Justification { get; set; }
    public string? RequiredSkills { get; set; }
    public int? MinExperience { get; set; }
    public int? MaxExperience { get; set; }
    public string? EducationLevel { get; set; }
    public decimal? SalaryRangeMin { get; set; }
    public decimal? SalaryRangeMax { get; set; }
    public DateTime? ExpectedStartDate { get; set; }
    public string Status { get; set; } = null!;
    public string? RequestedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
