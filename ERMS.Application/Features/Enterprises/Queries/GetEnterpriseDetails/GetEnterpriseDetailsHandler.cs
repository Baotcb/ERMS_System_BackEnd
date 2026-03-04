using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Queries.GetEnterpriseDetails;

public class GetEnterpriseDetailsHandler : IRequestHandler<GetEnterpriseDetailsQuery, GetEnterpriseDetailsResponse>
{
    private readonly IERMSDbContext _context;
    private readonly ILogger<GetEnterpriseDetailsHandler> _logger;

    public GetEnterpriseDetailsHandler(IERMSDbContext context, ILogger<GetEnterpriseDetailsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetEnterpriseDetailsResponse> Handle(GetEnterpriseDetailsQuery request, CancellationToken cancellationToken)
    {
        var enterprise = await _context.Enterprises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => 
                e.Id == request.Id && 
                !e.IsDeleted && 
                e.Status == "Active", 
                cancellationToken);

        if (enterprise == null)
        {
            _logger.LogWarning("Enterprise Details request failed: Enterprise with ID {EnterpriseId} was not found, deleted, or is inactive.", request.Id);
            throw new Exception($"Enterprise with ID {request.Id} not found or is currently unavailable.");
        }

        return new GetEnterpriseDetailsResponse
        {
            Id = enterprise.Id,
            EnterpriseName = enterprise.EnterpriseName,
            EnterpriseCode = enterprise.EnterpriseCode,
            Address = enterprise.Address,
            Phone = enterprise.Phone,
            Email = enterprise.Email,
            Website = enterprise.Website,
            LogoUrl = enterprise.LogoUrl
        };
    }
}
