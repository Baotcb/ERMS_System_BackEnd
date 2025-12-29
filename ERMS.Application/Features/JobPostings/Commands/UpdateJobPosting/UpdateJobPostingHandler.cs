using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting
{
    public class UpdateJobPostingHandler : IRequestHandler<UpdateJobPostingCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateJobPostingHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateJobPostingCommand request, CancellationToken cancellationToken)
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

            // Kiểm tra quyền (chỉ creator hoặc admin mới được sửa)
            if (jobPosting.CreatorId != userId.Value)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài đăng này.");
            }

            jobPosting.Title = request.Title;
            jobPosting.Description = request.Description;
            jobPosting.Requirements = request.Requirements;
            jobPosting.MinSalary = request.MinSalary;
            jobPosting.MaxSalary = request.MaxSalary;
            jobPosting.Currency = request.Currency;
            jobPosting.Location = request.Location;
            jobPosting.DepartmentId = request.DepartmentId;
            jobPosting.PostingType = request.PostingType;
            jobPosting.Status = request.Status;
            jobPosting.PublishDate = request.PublishDate;
            jobPosting.ExpiresAt = request.ExpiresAt;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}