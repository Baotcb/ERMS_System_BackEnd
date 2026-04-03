using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Applications.Commands.RejectOffer;

/// <summary>
/// Handler for rejecting a job offer.
/// Only the owning Candidate can reject their own offer.
/// </summary>
public sealed class RejectOfferHandler : IRequestHandler<RejectOfferCommand, RejectOfferResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RejectOfferHandler> _logger;

    public RejectOfferHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<RejectOfferHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RejectOfferResult> Handle(RejectOfferCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Chỉ ứng viên mới có quyền từ chối đề nghị.");
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

        // 6. Validate offer status - only "Sent" offers can be rejected
        if (!OfferStatus.CanRespond(offer.Status))
        {
            throw new Exception($"Đề nghị này không thể bị từ chối. Trạng thái hiện tại là '{offer.Status}'. Chỉ đề nghị có trạng thái 'Sent' mới có thể từ chối.");
        }

        var candidateNote = request.CandidateNote.Trim();
        var respondedAt = DateTime.UtcNow;

        // 7. Update Offer
        offer.Status = OfferStatus.Rejected;
        offer.RespondedAt = respondedAt;
        offer.CandidateNote = candidateNote;
        offer.UpdatedAt = respondedAt;

        // 8. Update Application stage to Rejected
        var application = offer.Application;
        application.Stage = ApplicationStage.Rejected;
        application.RejectedAt = respondedAt;
        application.RejectedById = userId;
        application.RejectionReason = candidateNote;
        application.StageUpdatedAt = respondedAt;
        application.UpdatedAt = respondedAt;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Offer {OfferId} rejected by candidate {CandidateId} (UserId: {UserId}). Application {ApplicationId} moved to {Stage}.",
            offer.Id,
            candidate.Id,
            userId,
            offer.ApplicationId,
            application.Stage);

        return new RejectOfferResult
        {
            OfferId = offer.Id,
            NewOfferStatus = OfferStatus.Rejected,
            RespondedAt = offer.RespondedAt ?? DateTime.UtcNow
        };
    }
}
