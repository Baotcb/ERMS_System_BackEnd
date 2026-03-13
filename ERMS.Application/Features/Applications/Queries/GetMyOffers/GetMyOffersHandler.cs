using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Queries.GetMyOffers;

/// <summary>
/// Handler for retrieving a candidate's own offers.
/// Only the authenticated Candidate can view their offers.
/// </summary>
public sealed class GetMyOffersHandler : IRequestHandler<GetMyOffersQuery, GetMyOffersResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetMyOffersHandler> _logger;

    public GetMyOffersHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetMyOffersHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetMyOffersResponse> Handle(GetMyOffersQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền xem đề nghị của mình.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy hồ sơ ứng viên.");

        // 4. Query offers through the Application relationship
        var query = _context.Offers
            .Include(o => o.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(o => o.Department)
            .Where(o => o.Application.CandidateId == candidate.Id
                     && !o.IsDeleted
                     && !o.Application.IsDeleted)
            .OrderByDescending(o => o.CreatedAt);

        // 5. Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 6. Paginate and project to DTO
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new CandidateOfferDto
            {
                OfferId = o.Id,
                OfferCode = o.OfferCode,
                Position = o.Position,
                DepartmentName = o.Department.DepartmentName,
                JobTitle = o.Application.JobPosting.JobTitle,
                Salary = o.Salary,
                SalaryFrequency = o.SalaryFrequency,
                Bonus = o.Bonus,
                Benefits = o.Benefits,
                StartDate = o.StartDate,
                ExpirationDate = o.ExpirationDate,
                OfferLetterUrl = o.OfferLetterUrl,
                Status = o.Status,
                SentAt = o.SentAt,
                RespondedAt = o.RespondedAt,
                CandidateNote = o.CandidateNote
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Candidate {CandidateId} (UserId: {UserId}) retrieved {Count} offers (Page {Page})",
            candidate.Id, userId, items.Count, request.PageNumber);

        return new GetMyOffersResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
