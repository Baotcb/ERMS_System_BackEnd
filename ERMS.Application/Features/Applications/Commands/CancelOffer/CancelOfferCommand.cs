using MediatR;

namespace ERMS.Application.Features.Applications.Commands.CancelOffer;

/// <summary>
/// Command for an HR Manager to cancel a job offer.
/// OfferId and CancellationReason are passed via the request body.
/// CancellationReason is used only for the notification email — it is NOT persisted to the database.
/// </summary>
public sealed class CancelOfferCommand : IRequest<CancelOfferResult>
{
    /// <summary>
    /// The Offer ID to cancel
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// Required reason for cancellation — sent to the candidate via email only, not saved to DB
    /// </summary>
    public string CancellationReason { get; set; } = null!;
}
