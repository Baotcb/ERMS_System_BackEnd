using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.DTO.Application
{
    public class ApplicationDto
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string? JobTitle { get; set; }
        public Guid CandidateId { get; set; }
        public string? CandidateName { get; set; }
        public string? CandidateEmail { get; set; }
        public Guid ResumeId { get; set; }
        public string? ResumeTitle { get; set; }
        public string CvUrl { get; set; } = string.Empty;
        public string? CoverLetter { get; set; }
        public double? MatchingScore { get; set; }
        public string? Category { get; set; }
        public string ApplicantType { get; set; } = "External";
        public string Status { get; set; } = "Applied";
        public DateTime AppliedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public string? WithdrawReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

