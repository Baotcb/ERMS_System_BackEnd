using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Commands.DeleteApplication
{
    public class DeleteApplicationHandler : IRequestHandler<DeleteApplicationCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteApplicationHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (application == null)
            {
                throw new Exception("Không tìm thấy đơn ứng tuyển.");
            }

            // Kiểm tra quyền: Chỉ candidate owner hoặc admin mới được xóa
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);

            if (candidate == null || application.CandidateId != candidate.UserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa đơn ứng tuyển này.");
            }

            // Chỉ cho phép xóa nếu status là Applied hoặc Withdrawn
            if (application.Status != "Applied" && application.Status != "Withdrawn")
            {
                throw new Exception("Không thể xóa đơn ứng tuyển đã được xử lý.");
            }

            _context.Applications.Remove(application);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

