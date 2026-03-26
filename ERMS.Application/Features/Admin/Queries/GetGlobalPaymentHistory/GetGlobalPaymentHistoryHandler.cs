using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Admin.Queries.GetGlobalPaymentHistory;

public sealed class GetGlobalPaymentHistoryHandler : IRequestHandler<GetGlobalPaymentHistoryQuery, GetGlobalPaymentHistoryResponse>
{
    private readonly IERMSDbContext _context;

    public GetGlobalPaymentHistoryHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetGlobalPaymentHistoryResponse> Handle(GetGlobalPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SubscriptionHistories
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EnterpriseSearch))
        {
            var search = request.EnterpriseSearch.Trim().ToLower();
            query = query.Where(history =>
                history.Enterprise.EnterpriseName.ToLower().Contains(search) ||
                history.Enterprise.EnterpriseCode.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            query = query.Where(history => history.ActionType == request.ActionType);
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            query = query.Where(history => history.PaymentMethod == request.PaymentMethod);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(history => history.CreatedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            var inclusiveDateTo = request.DateTo.Value.Date.AddDays(1);
            query = query.Where(history => history.CreatedAt < inclusiveDateTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var items = await query
            .OrderByDescending(history => history.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(history => new GlobalPaymentHistoryItemDto
            {
                Id = history.Id,
                EnterpriseId = history.EnterpriseId,
                EnterpriseName = history.Enterprise.EnterpriseName,
                EnterpriseCode = history.Enterprise.EnterpriseCode,
                ActionType = history.ActionType,
                PlanName = history.SubscriptionPlan.PlanName,
                PlanCode = history.SubscriptionPlan.PlanCode,
                PreviousPlanName = history.PreviousPlan != null ? history.PreviousPlan.PlanName : null,
                Amount = history.Amount,
                Currency = history.Currency,
                PaymentMethod = history.PaymentMethod,
                PaymentReference = history.PaymentReference,
                PeriodStartDate = history.PeriodStartDate,
                PeriodEndDate = history.PeriodEndDate,
                Note = history.Note,
                CreatedAt = history.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetGlobalPaymentHistoryResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
