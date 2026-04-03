using MediatR;

namespace ERMS.Application.Features.Admin.Queries.GetEnterpriseAdminDetail;

public sealed class GetEnterpriseAdminDetailQuery : IRequest<GetEnterpriseAdminDetailResponse>
{
    public Guid EnterpriseId { get; set; }
}
