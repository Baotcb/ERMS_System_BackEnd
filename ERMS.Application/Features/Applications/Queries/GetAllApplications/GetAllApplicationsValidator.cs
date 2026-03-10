using FluentValidation;
using ERMS.Domain.Constants.Application;

namespace ERMS.Application.Features.Applications.Queries.GetAllApplications;

/// <summary>
/// Validator for GetAllApplicationsQuery
/// </summary>
public sealed class GetAllApplicationsValidator : AbstractValidator<GetAllApplicationsQuery>
{
    public GetAllApplicationsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải ít nhất là 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Kích thước trang phải từ 1 đến 100.");

        RuleFor(x => x.StageFilter)
            .Must(stage => string.IsNullOrEmpty(stage) || ApplicationStage.IsValid(stage))
            .WithMessage("Bộ lọc giai đoạn phải hợp lệ.");
    }
}
