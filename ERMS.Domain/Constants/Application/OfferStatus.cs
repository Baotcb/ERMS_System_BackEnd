namespace ERMS.Domain.Constants.Application;

/// <summary>
/// Status constants for the Offer entity
/// </summary>
public static class OfferStatus
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";

    public static readonly string[] ValidStatuses =
        [Draft, PendingApproval, Approved, Sent, Accepted, Rejected, Expired, Cancelled];

    public static bool IsValid(string status)
        => Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns true if the offer can be responded to by a candidate (Accept/Reject).
    /// Only offers with status "Sent" can be responded to.
    /// </summary>
    public static bool CanRespond(string status)
        => status.Equals(Sent, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if the offer can be cancelled by an HR Manager.
    /// Only "Sent" and "Accepted" offers can be cancelled.
    /// </summary>
    public static bool CanCancel(string status)
        => status.Equals(Sent, StringComparison.OrdinalIgnoreCase)
        || status.Equals(Accepted, StringComparison.OrdinalIgnoreCase);
}
