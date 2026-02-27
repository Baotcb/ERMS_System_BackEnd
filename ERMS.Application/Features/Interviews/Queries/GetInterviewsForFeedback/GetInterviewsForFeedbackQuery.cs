using ERMS.Application.Features.Interviews.Queries.GetInterviewsForFeedback;
using MediatR;

namespace ERMS.Application.Features.Interviews.Queries.GetInterviewsForFeedback;

/// <summary>
/// Query for Department Head to retrieve completed interviews that have received feedback.
/// </summary>
public sealed class GetInterviewsForFeedbackQuery : IRequest<GetInterviewsForFeedbackResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
