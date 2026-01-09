using ERMS.Application.Features.Applications.DTOs;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Queries.GetAllApplications
{
    public class GetAllApplicationsHandler : IRequestHandler<GetAllApplicationsQuery, PagedResponse<ApplicationDto>>
    {
        private readonly IERMSDbContext _context;

        public GetAllApplicationsHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<ApplicationDto>> Handle(GetAllApplicationsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Applications
                .Include(a => a.Job)
                .Include(a => a.Candidate)
                    .ThenInclude(c => c.User)
                .Include(a => a.Resume)
                .AsQueryable();

            // Filter by JobId
            if (request.JobId.HasValue)
            {
                query = query.Where(a => a.JobId == request.JobId.Value);
            }

            // Filter by CandidateId
            if (request.CandidateId.HasValue)
            {
                query = query.Where(a => a.CandidateId == request.CandidateId.Value);
            }

            // Filter by Status
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(a => a.Status == request.Status);
            }

            // Filter by ApplicantType
            if (!string.IsNullOrEmpty(request.ApplicantType))
            {
                query = query.Where(a => a.ApplicantType == request.ApplicantType);
            }

            // Filter by Category
            if (!string.IsNullOrEmpty(request.Category))
            {
                query = query.Where(a => a.Category == request.Category);
            }

            // Search: Tìm kiếm theo JobTitle, CandidateName, CandidateEmail
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(a =>
                    (a.Job != null && a.Job.Title.ToLower().Contains(searchTerm)) ||
                    (a.Candidate != null && a.Candidate.User != null && 
                     (!string.IsNullOrEmpty(a.Candidate.User.FullName) && a.Candidate.User.FullName.ToLower().Contains(searchTerm))) ||
                    (a.Candidate != null && a.Candidate.User != null && 
                     (!string.IsNullOrEmpty(a.Candidate.User.Email) && a.Candidate.User.Email.ToLower().Contains(searchTerm)))
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Sorting
            switch (request.SortBy?.ToLower())
            {
                case "matchingscore":
                    query = request.SortDescending
                        ? query.OrderByDescending(a => a.MatchingScore ?? 0)
                        : query.OrderBy(a => a.MatchingScore ?? 0);
                    break;
                case "createdat":
                    query = request.SortDescending
                        ? query.OrderByDescending(a => a.CreatedAt)
                        : query.OrderBy(a => a.CreatedAt);
                    break;
                case "appliedat":
                default:
                    query = request.SortDescending
                        ? query.OrderByDescending(a => a.AppliedAt)
                        : query.OrderBy(a => a.AppliedAt);
                    break;
            }

            // Pagination
            var applications = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var applicationDtos = applications.Select(a => new ApplicationDto
            {
                Id = a.Id,
                JobId = a.JobId,
                JobTitle = a.Job?.Title,
                CandidateId = a.CandidateId,
                CandidateName = a.Candidate?.User?.FullName,
                CandidateEmail = a.Candidate?.User?.Email,
                ResumeId = a.ResumeId,
                ResumeTitle = a.Resume?.Title,
                CvUrl = a.CvUrl,
                CoverLetter = a.CoverLetter,
                MatchingScore = a.MatchingScore,
                Category = a.Category,
                ApplicantType = a.ApplicantType,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                WithdrawnAt = a.WithdrawnAt,
                WithdrawReason = a.WithdrawReason,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            }).ToList();

            return new PagedResponse<ApplicationDto>
            {
                Data = applicationDtos,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}

