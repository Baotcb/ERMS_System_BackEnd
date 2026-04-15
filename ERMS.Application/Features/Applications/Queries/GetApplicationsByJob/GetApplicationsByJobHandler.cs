using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationsByJob;

/// <summary>
/// Handler for retrieving applications for a specific job posting, sorted by AI match score
/// </summary>
public sealed class GetApplicationsByJobHandler : IRequestHandler<GetApplicationsByJobQuery, GetApplicationsByJobResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetApplicationsByJobHandler(IERMSDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetApplicationsByJobResponse> Handle(GetApplicationsByJobQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");


        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");
        
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || (!userRoles.Contains(AppRoles.HRManager) && !userRoles.Contains(AppRoles.Director)))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager hoặc Giám đốc mới có quyền xem hồ sơ ứng tuyển.");
        }



        // 4. Validate job posting exists and belongs to enterprise
        var jobPosting = await _context.JobPostings
            .Where(jp => jp.Id == request.JobPostingId && jp.EnterpriseId == enterpriseId && !jp.IsDeleted)
            .Select(jp => new { jp.Id, jp.JobTitle })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Exception($"Không tìm thấy tin tuyển dụng với ID {request.JobPostingId}.");

        // 5. Build query for applications
        var query = _context.Applications
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .Include(a => a.ExternalCandidate)
            .Include(a => a.Resume)
            .Include(a => a.CVScreeningResult)
            .Where(a => a.JobPostingId == request.JobPostingId && !a.IsDeleted);

        // 6. Optional stage filter
        if (!string.IsNullOrWhiteSpace(request.StageFilter))
        {
            query = query.Where(a => a.Stage == request.StageFilter);
        }

        // 7. Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 8. Sort by CV Score descending (highest first), then by AppliedAt
        var dbItems = await query
            .OrderByDescending(a => a.CVScreeningResult != null ? a.CVScreeningResult.OverallScore : 0)
            .ThenByDescending(a => a.AppliedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                ApplicationId = a.Id,
                CandidateId = a.CandidateId,
                CandidateName = a.ExternalCandidateId != null && a.ExternalCandidate != null
                    ? a.ExternalCandidate.FullName
                    : a.Candidate.User.FullName,
                CandidateEmail = a.ExternalCandidateId != null && a.ExternalCandidate != null
                    ? a.ExternalCandidate.Email
                    : a.Candidate.User.Email,
                CandidatePhone = a.ExternalCandidateId != null && a.ExternalCandidate != null
                    ? a.ExternalCandidate.PhoneNumber
                    : a.Candidate.User.PhoneNumber,
                ResumeUrl = a.Resume != null ? a.Resume.FileUrl : null,
                Stage = a.Stage,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                HRNote = a.HRNote,
                IsExternal = a.ExternalCandidateId != null,
                Source = a.Source,
                CVScreeningResult = a.CVScreeningResult
            })
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(x => new ApplicationListDto
        {
            ApplicationId = x.ApplicationId,
            CandidateId = x.CandidateId,
            CandidateName = x.CandidateName,
            CandidateEmail = x.CandidateEmail,
            CandidatePhone = x.CandidatePhone,
            ResumeUrl = x.ResumeUrl,
            Stage = x.Stage,
            Status = x.Status,
            AppliedAt = x.AppliedAt,
            HRNote = x.HRNote,
            IsExternal = x.IsExternal,
            Source = x.Source,
            // CV Screening Result mapping
            OverallScore = x.CVScreeningResult?.OverallScore,
            SkillMatchScore = x.CVScreeningResult?.SkillMatchScore,
            ExperienceMatchScore = x.CVScreeningResult?.ExperienceMatchScore,
            EducationMatchScore = x.CVScreeningResult?.EducationMatchScore,
            KeywordMatchScore = x.CVScreeningResult?.KeywordMatchScore,
            MatchedSkills = !string.IsNullOrEmpty(x.CVScreeningResult?.MatchedSkills) ? JsonSerializer.Deserialize<List<string>>(x.CVScreeningResult.MatchedSkills, (JsonSerializerOptions?)null) : null,
            MissingSkills = !string.IsNullOrEmpty(x.CVScreeningResult?.MissingSkills) ? JsonSerializer.Deserialize<List<string>>(x.CVScreeningResult.MissingSkills, (JsonSerializerOptions?)null) : null,
            Strengths = !string.IsNullOrEmpty(x.CVScreeningResult?.Strengths) ? JsonSerializer.Deserialize<List<string>>(x.CVScreeningResult.Strengths, (JsonSerializerOptions?)null) : null,
            Concerns = !string.IsNullOrEmpty(x.CVScreeningResult?.Concerns) ? JsonSerializer.Deserialize<List<string>>(x.CVScreeningResult.Concerns, (JsonSerializerOptions?)null) : null,
            AISummary = x.CVScreeningResult?.Summary
        }).ToList();

        return new GetApplicationsByJobResponse
        {
            JobPostingId = jobPosting.Id,
            JobTitle = jobPosting.JobTitle,
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
