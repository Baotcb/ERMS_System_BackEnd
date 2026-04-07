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
            _logger.LogWarning("Yêu cầu chi tiết doanh nghiệp thất bại: Doanh nghiệp với ID {EnterpriseId} không tìm thấy, đã bị xóa hoặc không hoạt động.", request.Id);
            throw new Exception($"Không tìm thấy doanh nghiệp với ID {request.Id} hoặc hiện không khả dụng.");
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
