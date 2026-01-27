using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Commands.UpdateEnterprise
{
    public class UpdateEnterpriseHandler : IRequestHandler<UpdateEnterpriseCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateEnterpriseHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateEnterpriseCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

            if (enterprise == null)
            {
                throw new Exception("Không tìm thấy doanh nghiệp.");
            }

            if (enterprise.IsDeleted)
            {
                throw new Exception("Không thể cập nhật doanh nghiệp đã bị xóa.");
            }

            // Kiểm tra quyền: Chỉ admin hoặc người tạo mới được update
            // Có thể thêm logic check role ở đây nếu cần

            // Update EnterpriseCode nếu có thay đổi
            if (!string.IsNullOrEmpty(request.EnterpriseCode) && request.EnterpriseCode != enterprise.EnterpriseCode)
            {
                var existingEnterprise = await _context.Enterprises
                    .FirstOrDefaultAsync(e => e.EnterpriseCode == request.EnterpriseCode && e.Id != request.Id, cancellationToken);

                if (existingEnterprise != null)
                {
                    throw new Exception("Mã doanh nghiệp đã tồn tại.");
                }
                enterprise.EnterpriseCode = request.EnterpriseCode;
            }

            // Update SubscriptionPlan nếu có thay đổi
            if (request.SubscriptionPlanId.HasValue)
            {
                var subscriptionPlan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(sp => sp.Id == request.SubscriptionPlanId.Value, cancellationToken);

                if (subscriptionPlan == null)
                {
                    throw new Exception("Không tìm thấy gói đăng ký.");
                }
                enterprise.SubscriptionPlanId = request.SubscriptionPlanId.Value;
            }

            // Update SubscriptionStatus nếu có thay đổi
            if (!string.IsNullOrEmpty(request.SubscriptionStatus))
            {
                var validStatuses = new[] { "Active", "Expired", "Cancelled", "Trial", "PastDue" };
                if (!validStatuses.Contains(request.SubscriptionStatus))
                {
                    throw new Exception($"Trạng thái đăng ký không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}");
                }
                enterprise.SubscriptionStatus = request.SubscriptionStatus;
            }

            // Update các fields khác
            if (!string.IsNullOrEmpty(request.EnterpriseName))
            {
                enterprise.EnterpriseName = request.EnterpriseName;
            }

            if (request.TaxCode != null)
            {
                enterprise.TaxCode = request.TaxCode;
            }

            if (request.Address != null)
            {
                enterprise.Address = request.Address;
            }

            if (request.Phone != null)
            {
                enterprise.Phone = request.Phone;
            }

            if (request.Email != null)
            {
                enterprise.Email = request.Email;
            }

            if (request.Website != null)
            {
                enterprise.Website = request.Website;
            }

            if (request.LogoUrl != null)
            {
                enterprise.LogoUrl = request.LogoUrl;
            }

            if (request.SubscriptionStartDate.HasValue)
            {
                enterprise.SubscriptionStartDate = request.SubscriptionStartDate.Value;
            }

            if (request.SubscriptionEndDate.HasValue)
            {
                enterprise.SubscriptionEndDate = request.SubscriptionEndDate.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
