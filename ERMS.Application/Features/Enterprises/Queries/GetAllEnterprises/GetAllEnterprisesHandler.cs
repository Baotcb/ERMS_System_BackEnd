using ERMS.Application.Features.Enterprises.DTOs;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Queries.GetAllEnterprises
{
    public class GetAllEnterprisesHandler : IRequestHandler<GetAllEnterprisesQuery, PagedResponse<EnterpriseDto>>
    {
        private readonly IERMSDbContext _context;

        public GetAllEnterprisesHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<EnterpriseDto>> Handle(GetAllEnterprisesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Enterprises
                .Include(e => e.SubscriptionPlan)
                .Include(e => e.CreatedBy)
                .AsQueryable();

            // Filter by IsDeleted
            if (!request.IncludeDeleted)
            {
                query = query.Where(e => !e.IsDeleted);
            }

            // Filter by SubscriptionPlanId
            if (request.SubscriptionPlanId.HasValue)
            {
                query = query.Where(e => e.SubscriptionPlanId == request.SubscriptionPlanId.Value);
            }

            // Filter by SubscriptionStatus
            if (!string.IsNullOrEmpty(request.SubscriptionStatus))
            {
                query = query.Where(e => e.SubscriptionStatus == request.SubscriptionStatus);
            }

            // Search: Tìm kiếm theo EnterpriseName, EnterpriseCode, Email
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(e =>
                    e.EnterpriseName.ToLower().Contains(searchTerm) ||
                    e.EnterpriseCode.ToLower().Contains(searchTerm) ||
                    (!string.IsNullOrEmpty(e.Email) && e.Email.ToLower().Contains(searchTerm))
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Sorting
            switch (request.SortBy?.ToLower())
            {
                case "enterprisename":
                    query = request.SortDescending
                        ? query.OrderByDescending(e => e.EnterpriseName)
                        : query.OrderBy(e => e.EnterpriseName);
                    break;
                case "subscriptionenddate":
                    query = request.SortDescending
                        ? query.OrderByDescending(e => e.SubscriptionEndDate)
                        : query.OrderBy(e => e.SubscriptionEndDate);
                    break;
                case "createdat":
                default:
                    query = request.SortDescending
                        ? query.OrderByDescending(e => e.CreatedAt)
                        : query.OrderBy(e => e.CreatedAt);
                    break;
            }

            // Pagination
            var enterprises = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var enterpriseDtos = enterprises.Select(e => new EnterpriseDto
            {
                Id = e.Id,
                EnterpriseName = e.EnterpriseName,
                EnterpriseCode = e.EnterpriseCode,
                TaxCode = e.TaxCode,
                Address = e.Address,
                Phone = e.Phone,
                Email = e.Email,
                Website = e.Website,
                LogoUrl = e.LogoUrl,
                SubscriptionPlanId = e.SubscriptionPlanId,
                SubscriptionPlanName = e.SubscriptionPlan?.PlanName,
                SubscriptionPlanCode = e.SubscriptionPlan?.PlanCode,
                SubscriptionStartDate = e.SubscriptionStartDate,
                SubscriptionEndDate = e.SubscriptionEndDate,
                SubscriptionStatus = e.SubscriptionStatus,
                CreatedById = e.CreatedById,
                CreatedByName = e.CreatedBy?.FullName,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                IsDeleted = e.IsDeleted,
                DeletedAt = e.DeletedAt
            }).ToList();

            return new PagedResponse<EnterpriseDto>
            {
                Data = enterpriseDtos,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
