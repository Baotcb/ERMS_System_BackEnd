using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Enterprise;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Enterprises.Commands.LockEnterprise
{
    public class LockEnterpriseHandler : IRequestHandler<LockEnterpriseCommand, bool>
    {
       private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public LockEnterpriseHandler(IERMSDbContext context,ICurrentUserService currentUserService) 
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(LockEnterpriseCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }
            var userRoles = _currentUserService.Roles;
            if (userRoles == null || !userRoles.Contains(AppRoles.Admin))
            {
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền chỉnh sửa trạng thái doanh nghiệp .");
            }
            var enterprise = await _context.Enterprises.FindAsync(request.EnterpriseId);
            if (request.IsLocked)
            {
                enterprise.Status = Domain.Constants.Enterprise.EnterpriseStatus.Locked;
            }
            else
            {
                enterprise.Status = Domain.Constants.Enterprise.EnterpriseStatus.Active;
            }
            _context.Enterprises.Update(enterprise);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
