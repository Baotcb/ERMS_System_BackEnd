using System;

namespace ERMS.Application.Features.Enterprises.Queries.GetEnterpriseDetails;

/// <summary>
/// Public-facing enterprise details. Sensitive data such as TaxCode or Subscription Status is filtered out.
/// </summary>
public record GetEnterpriseDetailsResponse
{
    public Guid Id { get; init; }
    public string EnterpriseName { get; init; } = string.Empty;
    public string EnterpriseCode { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? LogoUrl { get; init; }
}
