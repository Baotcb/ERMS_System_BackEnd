using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting
{
    public class DeleteJobPostingHandler : IRequestHandler<DeleteJobPostingCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteJobPostingHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteJobPostingCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

            if (jobPosting == null)
            {
                throw new Exception("Không tìm thấy bài đăng tuyển dụng.");
            }

            // Kiểm tra quyền
            if (jobPosting.CreatorId != userId.Value)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bài đăng này.");
            }

            _context.JobPostings.Remove(jobPosting);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}