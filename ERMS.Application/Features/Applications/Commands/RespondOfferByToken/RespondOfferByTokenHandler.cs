using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.RespondOfferByToken;

public sealed class RespondOfferByTokenHandler : IRequestHandler<RespondOfferByTokenCommand, RespondOfferByTokenResult>
{
    private readonly IERMSDbContext _context;
    private readonly ILogger<RespondOfferByTokenHandler> _logger;

    public RespondOfferByTokenHandler(IERMSDbContext context, ILogger<RespondOfferByTokenHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RespondOfferByTokenResult> Handle(RespondOfferByTokenCommand request, CancellationToken cancellationToken)
    {
        var action = request.Action.ToLower();
        var now = DateTime.UtcNow;
        var newStatus = action == "accept" ? OfferStatus.Accepted : OfferStatus.Rejected;

        // 1. Find offer by token — preliminary validity check
        var offer = await _context.Offers
            .Include(o => o.Application)
            .FirstOrDefaultAsync(o => o.ResponseToken == request.Token && !o.IsDeleted, cancellationToken);

        if (offer is null)
            return new RespondOfferByTokenResult { Success = false, Message = "Link không hợp lệ hoặc đã hết hạn." };

        if (offer.TokenExpiresAt.HasValue && offer.TokenExpiresAt.Value < now)
            return new RespondOfferByTokenResult { Success = false, Message = "Link đã hết hạn. Vui lòng liên hệ HR để được hỗ trợ." };

        if (offer.Status != OfferStatus.Sent)
            return new RespondOfferByTokenResult
            {
                Success = false,
                Message = "Offer này đã được phản hồi trước đó.",
                OfferStatus = offer.Status
            };

        // 2. Transaction + re-fetch to guard against race condition
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // Re-read inside the transaction so concurrent requests see the latest committed state
            var lockedOffer = await _context.Offers
                .Include(o => o.Application)
                .FirstOrDefaultAsync(o => o.Id == offer.Id && !o.IsDeleted, cancellationToken);

            if (lockedOffer is null || lockedOffer.Status != OfferStatus.Sent)
                return new RespondOfferByTokenResult
                {
                    Success = false,
                    Message = "Offer này đã được phản hồi trước đó."
                };

            // 3. Update offer fields
            lockedOffer.Status = newStatus;
            lockedOffer.ResponseToken = null;
            lockedOffer.TokenExpiresAt = null;
            lockedOffer.RespondedAt = now;
            lockedOffer.UpdatedAt = now;

            // 4. For reject: also update Application stage
            if (action == "reject")
            {
                lockedOffer.Application.Stage = ApplicationStage.Rejected;
                lockedOffer.Application.RejectedAt = now;
                lockedOffer.Application.StageUpdatedAt = now;
                lockedOffer.Application.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "External offer {OfferId} responded via token: action={Action}, newStatus={Status}",
                lockedOffer.Id, action, newStatus);

            return new RespondOfferByTokenResult
            {
                Success = true,
                Message = action == "accept"
                    ? "Bạn đã chấp nhận offer thành công. Bộ phận HR sẽ liên hệ bạn trong thời gian sớm nhất."
                    : "Offer đã được từ chối. Cảm ơn bạn đã phản hồi.",
                OfferStatus = newStatus
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Lỗi khi phản hồi offer {OfferId}", offer.Id);
            return new RespondOfferByTokenResult { Success = false, Message = "Đã xảy ra lỗi. Vui lòng thử lại." };
        }
    }
}
