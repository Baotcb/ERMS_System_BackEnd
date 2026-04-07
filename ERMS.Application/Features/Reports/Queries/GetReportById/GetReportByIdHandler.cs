using ERMS.Application.Interface;
using ERMS.Domain.Constants.System;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Reports.Queries.GetReportById;

public sealed class GetReportByIdHandler : IRequestHandler<GetReportByIdQuery, ReportDetailDto>
{
    private readonly IERMSDbContext _context;

    public GetReportByIdHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<ReportDetailDto> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
    {
        var report = await _context.Reports
            .AsNoTracking()
            .Include(x => x.ReportedBy)
            .Include(x => x.ResolvedBy)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Report was not found.");

        var (entityName, entityDetails) = await ResolveEntityInformationAsync(
            report.EntityType,
            report.EntityId,
            cancellationToken);

        var relatedReports = await _context.Reports
            .AsNoTracking()
            .Include(x => x.ReportedBy)
            .Where(x =>
                x.Id != report.Id
                && x.EntityType == report.EntityType
                && x.EntityId == report.EntityId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RelatedReportDto
            {
                Id = x.Id,
                Reason = x.Reason,
                ReportedByName = x.ReportedBy.FullName,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ReportDetailDto
        {
            Id = report.Id,
            EntityType = report.EntityType,
            EntityId = report.EntityId,
            EntityName = entityName,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status,
            ReportedByName = report.ReportedBy?.FullName ?? string.Empty,
            ReportedByEmail = report.ReportedBy?.Email ?? string.Empty,
            CreatedAt = report.CreatedAt,
            ResolvedByName = report.ResolvedBy?.FullName,
            ResolvedAt = report.ResolvedAt,
            AdminNote = report.AdminNote,
            ActionTaken = report.ActionTaken,
            EntityDetails = entityDetails,
            RelatedReports = relatedReports
        };
    }

    private async Task<(string EntityName, object? EntityDetails)> ResolveEntityInformationAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        if (entityType == ReportConstants.EntityType.JobPosting)
        {
            var jobPosting = await _context.JobPostings
                .AsNoTracking()
                .Where(x => x.Id == entityId)
                .Select(x => new
                {
                    x.Id,
                    x.JobTitle,
                    x.JobCode,
                    x.Status,
                    x.ApplicationDeadline,
                    x.Location,
                    x.EnterpriseId
                })
                .FirstOrDefaultAsync(cancellationToken);

            return jobPosting == null
                ? ("Unknown JobPosting", null)
                : (jobPosting.JobTitle, jobPosting);
        }

        if (entityType == ReportConstants.EntityType.Enterprise)
        {
            var enterprise = await _context.Enterprises
                .AsNoTracking()
                .Where(x => x.Id == entityId)
                .Select(x => new
                {
                    x.Id,
                    x.EnterpriseName,
                    x.EnterpriseCode,
                    x.Email,
                    x.Phone,
                    x.Status
                })
                .FirstOrDefaultAsync(cancellationToken);

            return enterprise == null
                ? ("Unknown Enterprise", null)
                : (enterprise.EnterpriseName, enterprise);
        }

        return ("Unknown Entity", null);
    }
}

