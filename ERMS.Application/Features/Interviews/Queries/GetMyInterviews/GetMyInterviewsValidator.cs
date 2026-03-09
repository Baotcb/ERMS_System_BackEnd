using FluentValidation;
using ERMS.Domain.Constants.Application;

namespace ERMS.Application.Features.Interviews.Queries.GetMyInterviews;

/// <summary>
/// Validator for GetMyInterviewsQuery
/// </summary>
public sealed class GetMyInterviewsValidator : AbstractValidator<GetMyInterviewsQuery>
{
    private static readonly HashSet<string> ValidStatuses = new()
    {
        InterviewStatus.PendingSchedule,
        InterviewStatus.Scheduled,
        InterviewStatus.Completed,
        InterviewStatus.Cancelled
    };

    public GetMyInterviewsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải ít nhất là 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Kích thước trang phải từ 1 đến 100.");

        RuleFor(x => x.StatusFilter)
            .Must(status => string.IsNullOrEmpty(status) || ValidStatuses.Contains(status))
            .WithMessage("Bộ lọc trạng thái phải hợp lệ (PendingSchedule, Scheduled, Completed, Cancelled).");
    }
}
