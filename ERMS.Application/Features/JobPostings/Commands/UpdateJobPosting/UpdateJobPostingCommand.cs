using MediatR;
using System;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting
{
    public class UpdateJobPostingCommand : IRequest<bool>
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
        public string PostingType { get; set; } = "External";
        public string Status { get; set; } = "Draft";
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}