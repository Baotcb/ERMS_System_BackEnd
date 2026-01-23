using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public class CreateEnterpriseHandler : IRequestHandler<CreateEnterpriseCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateEnterpriseHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(CreateEnterpriseCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            // Kiểm tra SubscriptionPlan tồn tại
            var subscriptionPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(sp => sp.Id == request.SubscriptionPlanId, cancellationToken);

            if (subscriptionPlan == null)
            {
                throw new Exception("Không tìm thấy gói đăng ký.");
            }

            // Kiểm tra EnterpriseCode đã tồn tại chưa
            var existingEnterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.EnterpriseCode == request.EnterpriseCode, cancellationToken);

            if (existingEnterprise != null)
            {
                throw new Exception("Mã doanh nghiệp đã tồn tại.");
            }

            // Validate SubscriptionStatus
            var validStatuses = new[] { "Active", "Expired", "Cancelled", "Trial", "PastDue" };
            if (!validStatuses.Contains(request.SubscriptionStatus))
            {
                throw new Exception($"Trạng thái đăng ký không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}");
            }

            var enterprise = new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = request.EnterpriseName,
                EnterpriseCode = request.EnterpriseCode,
                TaxCode = request.TaxCode,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Website = request.Website,
                LogoUrl = request.LogoUrl,
                SubscriptionPlanId = request.SubscriptionPlanId,
                SubscriptionStartDate = request.SubscriptionStartDate,
                SubscriptionEndDate = request.SubscriptionEndDate,
                SubscriptionStatus = request.SubscriptionStatus,
                CreatedById = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync(cancellationToken);

            return enterprise.Id;
        }
    }
}
