using ERMS.Application.Features.Enterprises.DTOs;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Queries.GetEnterpriseById
{
    public class GetEnterpriseByIdHandler : IRequestHandler<GetEnterpriseByIdQuery, EnterpriseDto>
    {
        private readonly IERMSDbContext _context;

        public GetEnterpriseByIdHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<EnterpriseDto> Handle(GetEnterpriseByIdQuery request, CancellationToken cancellationToken)
        {
            var enterprise = await _context.Enterprises
                .Include(e => e.SubscriptionPlan)
                .Include(e => e.CreatedBy)
                .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

            if (enterprise == null)
            {
                throw new System.Exception("Không tìm thấy doanh nghiệp.");
            }

            if(enterprise.IsDeleted)
            {
                throw new System.Exception("Doanh nghiệp đã bị xóa.");
            }

            return new EnterpriseDto
            {
                Id = enterprise.Id,
                EnterpriseName = enterprise.EnterpriseName,
                EnterpriseCode = enterprise.EnterpriseCode,
                TaxCode = enterprise.TaxCode,
                Address = enterprise.Address,
                Phone = enterprise.Phone,
                Email = enterprise.Email,
                Website = enterprise.Website,
                LogoUrl = enterprise.LogoUrl,
                SubscriptionPlanId = enterprise.SubscriptionPlanId,
                SubscriptionPlanName = enterprise.SubscriptionPlan?.PlanName,
                SubscriptionPlanCode = enterprise.SubscriptionPlan?.PlanCode,
                SubscriptionStartDate = enterprise.SubscriptionStartDate,
                SubscriptionEndDate = enterprise.SubscriptionEndDate,
                SubscriptionStatus = enterprise.SubscriptionStatus,
                CreatedById = enterprise.CreatedById,
                CreatedByName = enterprise.CreatedBy?.FullName,
                CreatedAt = enterprise.CreatedAt,
                UpdatedAt = enterprise.UpdatedAt,
            };
        }
    }
}
