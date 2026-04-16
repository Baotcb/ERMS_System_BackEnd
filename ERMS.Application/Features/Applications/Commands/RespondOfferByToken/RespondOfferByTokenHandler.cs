using ERMS.Application.Exceptions;
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

    public RespondOfferByTokenHandler(
        IERMSDbContext context,
        ILogger<RespondOfferByTokenHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RespondOfferByTokenResult> Handle(RespondOfferByTokenCommand request, CancellationToken cancellationToken)
    {
        var token = request.Token.Trim();
        var action = request.Action.Trim().ToLowerInvariant();

        if (action != "accept" && action != "reject")
        {
            throw new BusinessException("Action không hợp lệ. Chỉ chấp nhận 'accept' hoặc 'reject'.");
        }

        var offer = await _context.Offers
            .Include(o => o.Application)
            .FirstOrDefaultAsync(o =>
                !o.IsDeleted &&
                o.ResponseToken != null &&
                o.ResponseToken == token,
                cancellationToken)
            ?? throw new BusinessException("Offer token không hợp lệ hoặc đã được sử dụng.");

        if (!OfferStatus.CanRespond(offer.Status))
        {
            throw new BusinessException($"Không thể phản hồi offer ở trạng thái '{offer.Status}'.");
        }

        if (offer.TokenExpiresAt.HasValue && offer.TokenExpiresAt.Value < DateTime.UtcNow)
        {
            throw new BusinessException("Offer token đã hết hạn.");
        }

        var respondedAt = DateTime.UtcNow;
        if (action == "accept")
        {
            offer.Status = OfferStatus.Accepted;
        }
        else
        {
            offer.Status = OfferStatus.Rejected;
            offer.Application.Stage = ApplicationStage.Rejected;
            offer.Application.RejectedAt = respondedAt;
            offer.Application.StageUpdatedAt = respondedAt;
            offer.Application.UpdatedAt = respondedAt;
        }

        offer.RespondedAt = respondedAt;
        offer.UpdatedAt = respondedAt;
        offer.ResponseToken = null;
        offer.TokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Offer {OfferId} đã được phản hồi qua token với hành động '{Action}'", offer.Id, action);

        return new RespondOfferByTokenResult
        {
            OfferId = offer.Id,
            NewOfferStatus = offer.Status,
            RespondedAt = respondedAt
        };
    }
}
