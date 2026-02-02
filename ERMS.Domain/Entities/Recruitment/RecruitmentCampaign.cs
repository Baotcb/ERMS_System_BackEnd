using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Recruitment
{
    public class RecruitmentCampaign : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public string CampaignName { get; set; } = null!;
        public string CampaignCode { get; set; } = null!;
        public string? Description { get; set; }
        public int FiscalYear { get; set; }
        public byte? FiscalQuarter { get; set; }
        public DateTime SubmissionStartDate { get; set; }
        public DateTime SubmissionEndDate { get; set; }
        public DateTime? TargetHireStartDate { get; set; }
        public DateTime? TargetHireEndDate { get; set; }
        public decimal? TotalBudgetCeiling { get; set; }
        public int? MaxTotalPositions { get; set; }
        public string Status { get; set; } = "Draft";
        public Guid CreatedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<RecruitmentPlan> RecruitmentPlans { get; set; } = new List<RecruitmentPlan>();
    }
}
