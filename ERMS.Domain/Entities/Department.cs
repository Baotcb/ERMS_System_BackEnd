using ERMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class Department : BaseEntityInt
    {
        public Guid EnterpriseId { get; set; }
        public Enterprise Enterprise { get; set; } = null!;

        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Guid? ManagerId { get; set; }
        public Employee? Manager { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}
