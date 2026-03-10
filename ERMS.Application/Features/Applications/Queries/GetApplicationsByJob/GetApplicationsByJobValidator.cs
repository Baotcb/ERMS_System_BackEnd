using FluentValidation;
using ERMS.Domain.Constants.Application;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;

/// <summary>
/// Validator for GetApplicationsByJobQuery
/// </summary>
public sealed class GetApplicationsByJobValidator : AbstractValidator<GetApplicationsByJobQuery>
{
    public GetApplicationsByJobValidator()
    {
        RuleFor(x => x.JobPostingId)
            .NotEmpty()
            .WithMessage("JobPostingId là bắt buộc.");

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
