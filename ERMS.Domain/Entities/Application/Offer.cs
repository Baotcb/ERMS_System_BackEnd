using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Application
{
    public class Offer : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        public string? OfferCode { get; set; }
        public string Position { get; set; } = null!;
        public int DepartmentId { get; set; }
        public decimal Salary { get; set; }
        public string SalaryFrequency { get; set; } = "Monthly";
        public string? Bonus { get; set; }
        public string? Benefits { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? OfferLetterUrl { get; set; }
        public string Status { get; set; } = "Draft";
        public Guid CreatedById { get; set; }
        public Guid? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public Guid? SentById { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? CandidateNote { get; set; }
        public Guid? ResponseToken { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Application Application { get; set; } = null!;
        public virtual Department Department { get; set; } = null!;
        public virtual Identity.User CreatedBy { get; set; } = null!;
        public virtual Identity.User? ApprovedBy { get; set; }
        public virtual Identity.User? SentBy { get; set; }
    }
}
