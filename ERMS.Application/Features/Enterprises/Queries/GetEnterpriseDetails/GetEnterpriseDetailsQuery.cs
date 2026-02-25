using MediatR;
using System;

namespace ERMS.Application.Features.Enterprises.Queries.GetEnterpriseDetails;

/// <summary>
/// Query to retrieve public details of an Enterprise.
/// </summary>
public record GetEnterpriseDetailsQuery : IRequest<GetEnterpriseDetailsResponse>
{
    public Guid Id { get; init; }
}
