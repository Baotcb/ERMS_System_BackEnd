using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Organization
{
    public class Department : BaseEntityInt
    {
        public Guid EnterpriseId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public string? DepartmentCode { get; set; }
        public string? Description { get; set; }
        public Guid? ManagerId { get; set; }
        public int? ParentDepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
        public virtual Employee? Manager { get; set; }
        public virtual Department? ParentDepartment { get; set; }
        public virtual ICollection<Department> ChildDepartments { get; set; } = new List<Department>();
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
