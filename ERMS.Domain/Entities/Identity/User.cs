using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.System;

namespace ERMS.Domain.Entities.Identity
{
    public class User : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public string? Hometown { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public string? AvatarUrl { get; set; }

        public DateTime DateJoined { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int? DepartmentId { get; set; } 
        public Department? Department { get; set; }

        public Employee? Employee { get; set; }
        public Candidate.Candidate? Candidate { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
