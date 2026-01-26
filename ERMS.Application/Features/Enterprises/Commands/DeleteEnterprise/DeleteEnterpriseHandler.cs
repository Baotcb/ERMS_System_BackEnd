using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Commands.DeleteEnterprise
{
    public class DeleteEnterpriseHandler : IRequestHandler<DeleteEnterpriseCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteEnterpriseHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteEnterpriseCommand request, CancellationToken cancellationToken)
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
                throw new Exception("Doanh nghiệp đã bị xóa.");
            }

            // Soft delete
            enterprise.IsDeleted = true;
            enterprise.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
