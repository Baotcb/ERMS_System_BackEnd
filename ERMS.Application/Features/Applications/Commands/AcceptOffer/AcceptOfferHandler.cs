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
public sealed class AcceptOfferCommandHandler : IRequestHandler<AcceptOfferCommand, AcceptOfferResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AcceptOfferCommandHandler> _logger;

    public AcceptOfferCommandHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AcceptOfferCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AcceptOfferResult> Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 2. Role check: Candidate ONLY
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.Candidate))
        {
            throw new UnauthorizedAccessException("Only candidates can accept offers.");
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

        // 6. Validate offer status — only "Sent" offers can be accepted
        if (!OfferStatus.CanRespond(offer.Status))
        {
            throw new Exception($"Cannot accept offer. Current status is '{offer.Status}'. Only offers with status 'Sent' can be accepted.");
        }

        // 7. Validate offer is not expired
        if (offer.ExpirationDate < DateTime.UtcNow)
        {
            throw new Exception("Cannot accept offer. The offer has expired.");
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
