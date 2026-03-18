using FluentValidation;

namespace ERMS.Application.Features.Applications.Queries.GetShortlistedApplications;

/// <summary>
/// Validator for GetShortlistedApplicationsQuery
/// </summary>
public sealed class GetShortlistedApplicationsValidator : AbstractValidator<GetShortlistedApplicationsQuery>
{
    public GetShortlistedApplicationsValidator()
    {
        RuleFor(x => x.PlanDetailId)
            .NotEmpty()
            .WithMessage("PlanDetailId là bắt buộc.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải ít nhất là 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
