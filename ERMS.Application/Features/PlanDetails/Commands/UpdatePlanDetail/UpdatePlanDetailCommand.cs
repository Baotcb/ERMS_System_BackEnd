using MediatR;

namespace ERMS.Application.Features.PlanDetails.Commands.UpdatePlanDetail;

public sealed class UpdatePlanDetailCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string PositionTitle { get; set; } = null!;
    public int Quantity { get; set; }
    public string Priority { get; set; } = "Normal";
    public string? Justification { get; set; }
    public string? RequiredSkills { get; set; }
    public int? MinExperience { get; set; }
    public int? MaxExperience { get; set; }
    public string? EducationLevel { get; set; }
    public decimal? SalaryRangeMin { get; set; }
    public decimal SalaryRangeMax { get; set; }
    public DateTime? ExpectedStartDate { get; set; }
}
