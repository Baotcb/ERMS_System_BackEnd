using ERMS.Application.Features.Applications.DTOs;
using MediatR;
using System;

namespace ERMS.Application.Features.Applications.Queries.GetAllApplications
{
    public class GetAllApplicationsQuery : IRequest<PagedResponse<ApplicationDto>>
    {
        public Guid? JobId { get; set; }
        public Guid? CandidateId { get; set; }
        public string? Status { get; set; }
        public string? ApplicantType { get; set; }
        public string? Category { get; set; }
        public string? SearchTerm { get; set; } // Tìm kiếm theo JobTitle, CandidateName, CandidateEmail
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } // AppliedAt, MatchingScore, CreatedAt
        public bool SortDescending { get; set; } = true;
    }
}

