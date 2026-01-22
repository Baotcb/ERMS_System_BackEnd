using ERMS.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Domain.Entities
{
    public class Employee : BaseEntity
    {
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;
        
        public int? DepartmentId { get; set; } // ✅ Đổi từ Guid? thành int?
        
        [StringLength(100)]
        public string? Position { get; set; }
        
        public DateTime JoinDate { get; set; }
        
        [StringLength(20)]
        public string? ContractType { get; set; }
        
        public DateTime? ContractEndDate { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Department? Department { get; set; }
    }
}