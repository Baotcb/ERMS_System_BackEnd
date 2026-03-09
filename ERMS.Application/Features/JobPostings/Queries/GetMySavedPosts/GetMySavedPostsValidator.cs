using FluentValidation;

namespace ERMS.Application.Features.JobPostings.Queries.GetMySavedPosts;

public sealed class GetMySavedPostsValidator : AbstractValidator<GetMySavedPostsQuery>
{
    public GetMySavedPostsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải ít nhất là 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("Kích thước trang phải từ 1 đến 50.");
    }
}
