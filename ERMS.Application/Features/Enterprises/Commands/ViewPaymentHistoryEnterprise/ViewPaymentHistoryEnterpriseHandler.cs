using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Interface;

namespace ERMS.Application.Features.Enterprises.Commands.ViewPaymentHistoryEnterprise
{
    public class ViewPaymentHistoryEnterpriseHandler : IRequestHandler<ViewPaymentHistoryEnterpriseCommand, ViewPaymentHistoryEnterpriseResponse>
    {
        private readonly IERMSDbContext _context;

        public ViewPaymentHistoryEnterpriseHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<ViewPaymentHistoryEnterpriseResponse> Handle(ViewPaymentHistoryEnterpriseCommand request, CancellationToken cancellationToken)
        {
            var histories = await _context.SubscriptionHistories
                .AsNoTracking()
                .Include(x => x.SubscriptionPlan)
                .Include(x => x.PreviousPlan)
                .Where(x => x.EnterpriseId == request.EnterpriseId)
                .OrderByDescending(x => x.PeriodStartDate)
                .Select(x => new SubscriptionHistoryDto
                {
                    Id = x.Id,
                    ActionType = x.ActionType,
                    PlanName = x.SubscriptionPlan.PlanName,
                    PreviousPlanName = x.PreviousPlan != null ? x.PreviousPlan.PlanName : null,
                    Amount = x.Amount,
                    Currency = x.Currency,
                    PaymentMethod = x.PaymentMethod,
                    PeriodStartDate = x.PeriodStartDate,
                    PeriodEndDate = x.PeriodEndDate,
                    Note = x.Note
                })
                .ToListAsync(cancellationToken);
            if (!histories.Any())
            {
               throw new Exception("No payment history found for the specified enterprise.");
            }
            return new ViewPaymentHistoryEnterpriseResponse
            {
                SubscriptionHistories = histories
            };


        }
    }
}
