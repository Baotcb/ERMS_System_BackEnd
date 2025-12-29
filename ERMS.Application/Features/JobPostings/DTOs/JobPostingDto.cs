using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.JobPostings.DTOs
{
    public class JobPostingDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "VND";
        public string? Location { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid CreatorId { get; set; }
        public string? CreatorName { get; set; }
        public string PostingType { get; set; } = "External";
        public string Status { get; set; } = "Draft";
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<SkillDto>? Skills { get; set; }
    }

    public class SkillDto
    {
        public int Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public int Weight { get; set; }
        public int MinProficiency { get; set; }
    }
}