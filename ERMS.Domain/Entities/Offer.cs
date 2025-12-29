using ERMS.Domain.Common;
using System;

namespace ERMS.Domain.Entities
{
    public class Offer : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public decimal Salary { get; set; }
        public string? Bonus { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? OfferLetterUrl { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Sent
    }
}