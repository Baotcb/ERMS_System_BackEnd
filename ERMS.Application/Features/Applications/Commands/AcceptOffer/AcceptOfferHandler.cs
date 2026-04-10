using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.AcceptOffer;

/// <summary>
/// Handler for accepting a job offer.
/// Only the owning Candidate can accept their own offer.
/// </summary>
public sealed class AcceptOfferHandler : IRequestHandler<AcceptOfferCommand, AcceptOfferResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AcceptOfferHandler> _logger;

    public AcceptOfferHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AcceptOfferHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AcceptOfferResult> Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền chấp nhận đề nghị.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Không tìm thấy hồ sơ ứng viên.");

        // 4. Load the Offer with its Application
        var offer = await _context.Offers
            .Include(o => o.Application)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId && !o.IsDeleted, cancellationToken)
            ?? throw new Exception($"Không tìm thấy đề nghị với ID {request.OfferId}.");

        // 5. IDOR check: Verify the offer belongs to this candidate
        if (offer.Application.CandidateId != candidate.Id)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập đề nghị này.");
        }

        // 6. Validate offer status — only "Sent" offers can be accepted
        if (!OfferStatus.CanRespond(offer.Status))
        {
            throw new Exception($"Không thể chấp nhận đề nghị. Trạng thái hiện tại là '{offer.Status}'. Chỉ đề nghị có trạng thái 'Sent' mới có thể chấp nhận.");
        }

        // 7. Validate offer is not expired
        if (offer.ExpirationDate < DateTime.UtcNow)
        {
            throw new Exception("Không thể chấp nhận đề nghị. Đề nghị đã hết hạn.");
        }

        // 8. Update Offer
        offer.Status = OfferStatus.Accepted;
        offer.RespondedAt = DateTime.UtcNow;
        offer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Offer {OfferId} accepted by candidate {CandidateId} (UserId: {UserId}).",
            offer.Id, candidate.Id, userId);

        return new AcceptOfferResult
        {
            OfferId = offer.Id,
            NewOfferStatus = OfferStatus.Accepted,
            ApplicationStage = offer.Application.Stage,
            RespondedAt = offer.RespondedAt ?? DateTime.UtcNow
        };
    }
}
