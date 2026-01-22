using ERMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERMS.Domain.Entities
{
    public class Department : BaseEntity
    {
        // ✅ Override Id thành int thay vì Guid từ BaseEntity
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public new int Id { get; set; }
        
        public Guid EnterpriseId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? DepartmentCode { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public Guid? ManagerId { get; set; }
        public int? ParentDepartmentId { get; set; } // ✅ Giờ đây tương thích với Id (int)
        
        public bool IsActive { get; set; } = true;
        
        // Navigation Properties
        public virtual Enterprise Enterprise { get; set; } = null!;
        public virtual User? Manager { get; set; }
        public virtual Department? ParentDepartment { get; set; }
        public virtual ICollection<Department> SubDepartments { get; set; } = new HashSet<Department>();
        public virtual ICollection<User> Users { get; set; } = new HashSet<User>();
        public virtual ICollection<Employee> Employees { get; set; } = new HashSet<Employee>();
        public virtual ICollection<PlanDetail> PlanDetails { get; set; } = new HashSet<PlanDetail>();
        public virtual ICollection<JobPosting> JobPostings { get; set; } = new HashSet<JobPosting>();
        public virtual ICollection<Offer> Offers { get; set; } = new HashSet<Offer>();
    }
}
