using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.JobPostings.Commands.SaveJobPosting
{
    public class SaveJobPostingHandler : IRequestHandler<SaveJobPostingCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SaveJobPostingHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(SaveJobPostingCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực");
            }

          
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(x => x.UserId == userId.Value, cancellationToken);

            if (candidate == null)
            {
                throw new InvalidOperationException("Không tìm thấy hồ sơ ứng viên");
            }
            if (request.JobPostingId == Guid.Empty)
            {
                throw new ArgumentException("ID tin tuyển dụng không hợp lệ");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(x => x.Id == request.JobPostingId, cancellationToken);
            if (jobPosting == null) {
                throw new InvalidOperationException("Không tìm thấy tin tuyển dụng");
            }

            var existingSavedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(x => x.CandidateId == candidate.Id && x.JobPostingId == request.JobPostingId, cancellationToken);

            if (existingSavedJob != null)
            {
                return existingSavedJob.Id;
            }

            var savedJob = new SavedJob
            {
                CandidateId = candidate.Id,
                JobPostingId = request.JobPostingId,
                SavedAt = DateTime.UtcNow
            };

            await _context.SavedJobs.AddAsync(savedJob, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return savedJob.Id;
        }
    }
}
