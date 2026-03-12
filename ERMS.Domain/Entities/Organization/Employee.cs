using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Organization
{
    public class Employee : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid EnterpriseId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? Position { get; set; }
        public Guid? JobPositionId { get; set; } 
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string EmploymentType { get; set; } = "FullTime";
        public Guid? ManagerId { get; set; }
        public decimal? Salary { get; set; }
        public bool IsTrainer { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Identity.User User { get; set; } = null!;
        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
        public virtual Department? Department { get; set; }
        public virtual Employee? Manager { get; set; } // Direct manager
        public virtual JobPosition? JobPosition { get; set; }
        public virtual ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    }
}
