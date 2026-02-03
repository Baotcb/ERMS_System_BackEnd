using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Recruitment
{
    public class RecruitmentPlan : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public Guid CampaignId { get; set; }
        public int DepartmentId { get; set; }
        public string PlanName { get; set; } = null!;
        public string PlanCode { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? TotalBudget { get; set; }
        public string Status { get; set; } = "Draft";
        public Guid CreatedById { get; set; }
        public Guid? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
        public virtual RecruitmentCampaign Campaign { get; set; } = null!;
        public virtual Organization.Department Department { get; set; } = null!;
        public virtual Identity.User CreatedBy { get; set; } = null!;
        public virtual Identity.User? ApprovedBy { get; set; }
        public virtual ICollection<PlanDetail> PlanDetails { get; set; } = new List<PlanDetail>();
    }
}
