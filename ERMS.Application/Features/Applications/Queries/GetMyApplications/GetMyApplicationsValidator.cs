using FluentValidation;

namespace ERMS.Application.Features.Applications.Queries.GetMyApplications;

public sealed class GetMyApplicationsValidator : AbstractValidator<GetMyApplicationsQuery>
{
    public GetMyApplicationsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải ít nhất là 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("Kích thước trang phải từ 1 đến 50.");
    }
}
