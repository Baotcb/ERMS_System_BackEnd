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
/// Application stage remains "Offered" (not changed to "Rejected").
/// </summary>
public sealed class RejectOfferCommandHandler : IRequestHandler<RejectOfferCommand, RejectOfferResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RejectOfferCommandHandler> _logger;

    public RejectOfferCommandHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<RejectOfferCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RejectOfferResult> Handle(RejectOfferCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Only candidates can reject offers.");
        }

        // 3. Resolve the Candidate profile from the current user
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, cancellationToken)
            ?? throw new Exception("Candidate profile not found.");

        // 4. Load the Offer with its Application
        var offer = await _context.Offers
            .Include(o => o.Application)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId && !o.IsDeleted, cancellationToken)
            ?? throw new Exception($"Offer with ID {request.OfferId} not found.");

        // 5. IDOR check: Verify the offer belongs to this candidate
        if (offer.Application.CandidateId != candidate.Id)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this offer.");
        }

        // 6. Validate offer status — only "Sent" offers can be rejected
        if (!OfferStatus.CanRespond(offer.Status))
        {
            throw new Exception($"Cannot reject offer. Current status is '{offer.Status}'. Only offers with status 'Sent' can be rejected.");
        }

        // 7. Update Offer
        offer.Status = OfferStatus.Rejected;
        offer.RespondedAt = DateTime.UtcNow;
        offer.CandidateNote = request.CandidateNote;
        offer.UpdatedAt = DateTime.UtcNow;

        // NOTE: Application.Stage intentionally NOT changed.
        // "Rejected" stage is reserved for HR-side rejection.
        // The application stays at "Offered" stage.

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Offer {OfferId} rejected by candidate {CandidateId} (UserId: {UserId}). Application {ApplicationId} stage remains unchanged.",
            offer.Id, candidate.Id, userId, offer.ApplicationId);

        return new RejectOfferResult
        {
            OfferId = offer.Id,
            NewOfferStatus = OfferStatus.Rejected,
            RespondedAt = offer.RespondedAt ?? DateTime.UtcNow
        };
    }
}
