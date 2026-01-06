using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.JobPostings.Commands.IncrementJobPostingViewCount
{
    public class IncrementJobPostingViewCountHandler : IRequestHandler<IncrementJobPostingViewCountCommand, bool>
    {
        private readonly IERMSDbContext _context;

        public IncrementJobPostingViewCountHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(IncrementJobPostingViewCountCommand request, CancellationToken cancellationToken)
        {
            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

            if (jobPosting == null)
            {
                throw new Exception("Không tìm thấy bài đăng tuyển dụng.");
            }

            jobPosting.ViewCount++;
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

