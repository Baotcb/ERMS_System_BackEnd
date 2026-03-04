using ERMS.Application.Interface;
using ERMS.Domain.Constants;
using ERMS.Domain.Constants.Roles;
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

        // Số tháng tối thiểu giữa các lần update
        private const int MinimumMonthsBetweenUpdates = 6;

        public UpdateEnterpriseHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateEnterpriseCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra người dùng đăng nhập
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            // 2. Kiểm tra quyền HR
            var userRoles = _currentUserService.Roles;
            if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
            {
                throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền cập nhật thông tin doanh nghiệp.");
            }

            // 3. Tìm doanh nghiệp
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

            // 4. Kiểm tra thời gian update - phải cách nhau ít nhất 6 tháng
            if (enterprise.UpdatedAt.HasValue)
            {
                var lastUpdated = enterprise.UpdatedAt.Value;
                var minimumNextUpdateDate = lastUpdated.AddMonths(MinimumMonthsBetweenUpdates);

                if (DateTime.UtcNow < minimumNextUpdateDate)
                {
                    var daysRemaining = (minimumNextUpdateDate - DateTime.UtcNow).Days;
                    throw new Exception($"Chưa đủ thời gian để cập nhật. Bạn cần đợi thêm {daysRemaining} ngày nữa (tối thiểu 6 tháng kể từ lần cập nhật trước: {lastUpdated:dd/MM/yyyy}).");
                }
            }

            // 5. Kiểm tra nếu có thay đổi tên doanh nghiệp
            if (!string.IsNullOrWhiteSpace(request.EnterpriseName) &&
                request.EnterpriseName.Trim() != enterprise.EnterpriseName)
            {
                var newName = request.EnterpriseName.Trim();

                // 5a. Kiểm tra tên không trùng với doanh nghiệp lớn (blacklist)
                if (ReservedEnterpriseNames.IsBlacklisted(newName))
                {
                    throw new Exception($"Tên doanh nghiệp '{newName}' không được phép sử dụng vì trùng hoặc tương tự với tên của các doanh nghiệp/tổ chức lớn.");
                }

                // 5b. Kiểm tra tên unique trong hệ thống
                var existingEnterprise = await _context.Enterprises
                    .FirstOrDefaultAsync(e =>
                        e.EnterpriseName.ToLower() == newName.ToLower() &&
                        e.Id != request.Id &&
                        !e.IsDeleted,
                        cancellationToken);

                if (existingEnterprise != null)
                {
                    throw new Exception($"Tên doanh nghiệp '{newName}' đã tồn tại trong hệ thống.");
                }

                enterprise.EnterpriseName = newName;
            }

            // 6. Cập nhật các trường được phép
            if (request.Address != null)
            {
                enterprise.Address = request.Address.Trim();
            }

            if (request.Phone != null)
            {
                enterprise.Phone = request.Phone.Trim();
            }

            if (request.Website != null)
            {
                enterprise.Website = request.Website.Trim();
            }

            // 7. Cập nhật thời gian sửa đổi
            enterprise.UpdatedAt = DateTime.UtcNow;

            // 8. Lưu thay đổi
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
