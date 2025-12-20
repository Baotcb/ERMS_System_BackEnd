using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;

        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int Status { get; set; } 
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    }
}
