using ERMS.Application.Features.Applications.DTOs;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationById
{
    public class GetApplicationByIdHandler : IRequestHandler<GetApplicationByIdQuery, ApplicationDto>
    {
        private readonly IERMSDbContext _context;

        public GetApplicationByIdHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationDto> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
        {
            var application = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.Candidate)
                    .ThenInclude(c => c.User)
                .Include(a => a.Resume)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (application == null)
            {
                throw new System.Exception("Không tìm thấy đơn ứng tuyển.");
            }

            return new ApplicationDto
            {
                Id = application.Id,
                JobId = application.JobId,
                JobTitle = application.Job?.Title,
                CandidateId = application.CandidateId,
                CandidateName = application.Candidate?.User?.FullName,
                CandidateEmail = application.Candidate?.User?.Email,
                ResumeId = application.ResumeId,
                ResumeTitle = application.Resume?.Title,
                CvUrl = application.CvUrl,
                CoverLetter = application.CoverLetter,
                MatchingScore = application.MatchingScore,
                Category = application.Category,
                ApplicantType = application.ApplicantType,
                Status = application.Status,
                AppliedAt = application.AppliedAt,
                WithdrawnAt = application.WithdrawnAt,
                WithdrawReason = application.WithdrawReason,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            };
        }
    }
}

