using MediatR;

namespace ERMS.Application.Features.Training.Queries.GetDepartmentTrainingSummary
{
    public sealed class GetDepartmentTrainingSummaryQuery
        : IRequest<GetDepartmentTrainingSummaryResult>
    {
    }
}