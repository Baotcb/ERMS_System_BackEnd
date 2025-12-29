using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.JobPostings.Commands.CreateJobPosting
{
    public class CreateJobPostingCommand : IRequest<Guid>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "VND";
        public string? Location { get; set; }
        public int? DepartmentId { get; set; }
        public string PostingType { get; set; } = "External"; 
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<int>? SkillIds { get; set; } 
    }
}