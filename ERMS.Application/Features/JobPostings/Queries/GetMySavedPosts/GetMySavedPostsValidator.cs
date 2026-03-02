using FluentValidation;

namespace ERMS.Application.Features.JobPostings.Queries.GetMySavedPosts;

public sealed class GetMySavedPostsValidator : AbstractValidator<GetMySavedPostsQuery>
{
    public GetMySavedPostsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("PageSize must be between 1 and 50.");
    }
}
