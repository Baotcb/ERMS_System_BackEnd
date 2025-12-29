using System;

namespace ERMS.Application.Features.Users.DTO
{
    public class UserProfileDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Hometown { get; set; }
        public string? Phones { get; set; }
        public int? DepartmentId { get; set; } 
        public string? DepartmentName { get; set; }
        public int Status { get; set; }
        public DateTime DateJoined { get; set; }
    }
}
