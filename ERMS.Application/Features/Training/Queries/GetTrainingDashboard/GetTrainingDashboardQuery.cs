using MediatR;

namespace ERMS.Application.Features.Dashboard.Queries.GetTrainingDashboard
{
    public sealed class GetTrainingDashboardQuery
        : IRequest<GetTrainingDashboardResult>
    {
    }
}