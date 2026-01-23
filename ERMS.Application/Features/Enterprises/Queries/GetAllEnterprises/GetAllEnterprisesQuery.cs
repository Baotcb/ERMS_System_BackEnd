using ERMS.Application.Features.Enterprises.DTOs;
using MediatR;
using System;

namespace ERMS.Application.Features.Enterprises.Queries.GetAllEnterprises
{
    public class GetAllEnterprisesQuery : IRequest<PagedResponse<EnterpriseDto>>
    {
        public Guid? SubscriptionPlanId { get; set; }
        public string? SubscriptionStatus { get; set; }
        public string? SearchTerm { get; set; } // Tìm kiếm theo EnterpriseName, EnterpriseCode, Email
        public bool IncludeDeleted { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } // CreatedAt, EnterpriseName, SubscriptionEndDate
        public bool SortDescending { get; set; } = true;
    }
}
