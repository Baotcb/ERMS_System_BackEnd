using ERMS.Application.Interface;
using ERMS.Domain.Constants.System;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Reports.Queries.GetAllReports;

public sealed class GetAllReportsHandler : IRequestHandler<GetAllReportsQuery, GetAllReportsResult>
{
    private readonly IERMSDbContext _context;

    public GetAllReportsHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetAllReportsResult> Handle(GetAllReportsQuery request, CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, request.Page);
        var safePageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 200);

        var query = _context.Reports
            .AsNoTracking()
            .Include(x => x.ReportedBy)
            .Include(x => x.ResolvedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim();
            query = query.Where(x => x.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            var reason = request.Reason.Trim();
            query = query.Where(x => x.Reason == reason);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();

            var matchingJobPostingIds = _context.JobPostings
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.JobTitle.ToLower().Contains(search))
                .Select(x => x.Id)
                .AsQueryable();

            var matchingEnterpriseIds = _context.Enterprises
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.EnterpriseName.ToLower().Contains(search))
                .Select(x => x.Id)
                .AsQueryable();

            query = query.Where(x =>
                (x.ReportedBy.FullName != null && x.ReportedBy.FullName.ToLower().Contains(search))
                || (x.ReportedBy.Email != null && x.ReportedBy.Email.ToLower().Contains(search))
                || (x.EntityType == ReportConstants.EntityType.JobPosting && matchingJobPostingIds.Contains(x.EntityId))
                || (x.EntityType == ReportConstants.EntityType.Enterprise && matchingEnterpriseIds.Contains(x.EntityId)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var jobPostingIdsForPage = reports
            .Where(x => x.EntityType == ReportConstants.EntityType.JobPosting)
            .Select(x => x.EntityId)
            .Distinct()
            .ToList();

        var enterpriseIdsForPage = reports
            .Where(x => x.EntityType == ReportConstants.EntityType.Enterprise)
            .Select(x => x.EntityId)
            .Distinct()
            .ToList();

        var jobPostingMap = jobPostingIdsForPage.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.JobPostings
                .AsNoTracking()
                .Where(x => jobPostingIdsForPage.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.JobTitle, cancellationToken);

        var enterpriseMap = enterpriseIdsForPage.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Enterprises
                .AsNoTracking()
                .Where(x => enterpriseIdsForPage.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.EnterpriseName, cancellationToken);

        var counts = await _context.Reports
            .AsNoTracking()
            .Where(x =>
                (x.EntityType == ReportConstants.EntityType.JobPosting && jobPostingIdsForPage.Contains(x.EntityId))
                || (x.EntityType == ReportConstants.EntityType.Enterprise && enterpriseIdsForPage.Contains(x.EntityId)))
            .GroupBy(x => new { x.EntityType, x.EntityId })
            .Select(group => new ReportCountItem(group.Key.EntityType, group.Key.EntityId, group.Count()))
            .ToListAsync(cancellationToken);

        var countMap = counts.ToDictionary(
            x => BuildEntityKey(x.EntityType, x.EntityId),
            x => x.Count);

        var items = reports.Select(x => new ReportListDto
        {
            Id = x.Id,
            EntityType = x.EntityType,
            EntityId = x.EntityId,
            EntityName = ResolveEntityName(x.EntityType, x.EntityId, jobPostingMap, enterpriseMap),
            Reason = x.Reason,
            Description = x.Description,
            Status = x.Status,
            ReportedByName = x.ReportedBy?.FullName ?? string.Empty,
            ReportedByEmail = x.ReportedBy?.Email ?? string.Empty,
            CreatedAt = x.CreatedAt,
            ResolvedByName = x.ResolvedBy?.FullName,
            ResolvedAt = x.ResolvedAt,
            AdminNote = x.AdminNote,
            ActionTaken = x.ActionTaken,
            ReportCountForEntity = countMap.GetValueOrDefault(BuildEntityKey(x.EntityType, x.EntityId), 0)
        }).ToList();

        return new GetAllReportsResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = safePage,
            PageSize = safePageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize)
        };
    }

    private static string ResolveEntityName(
        string entityType,
        Guid entityId,
        IReadOnlyDictionary<Guid, string> jobPostingMap,
        IReadOnlyDictionary<Guid, string> enterpriseMap)
    {
        return entityType switch
        {
            var t when t == ReportConstants.EntityType.JobPosting =>
                jobPostingMap.GetValueOrDefault(entityId, "Unknown JobPosting"),
            var t when t == ReportConstants.EntityType.Enterprise =>
                enterpriseMap.GetValueOrDefault(entityId, "Unknown Enterprise"),
            _ => "Unknown Entity"
        };
    }

    private static string BuildEntityKey(string entityType, Guid entityId)
        => $"{entityType}:{entityId}";

    private sealed record ReportCountItem(string EntityType, Guid EntityId, int Count);
}
