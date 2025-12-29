using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace ERMS.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public string? Hometown { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Phones { get; set; }
        public string? AvatarUrl { get; set; }


        public int Status { get; set; } = 1;
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

  
        public int? DepartmentId { get; set; } 
        public Department? Department { get; set; }

        public Employee? Employee { get; set; }
        public Candidate? Candidate { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
