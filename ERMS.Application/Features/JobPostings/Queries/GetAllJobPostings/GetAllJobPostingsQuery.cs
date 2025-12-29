using ERMS.Application.Features.JobPostings.DTOs;
using MediatR;
using System.Collections.Generic;

namespace ERMS.Application.Features.JobPostings.Queries.GetAllJobPostings
{
    public class GetAllJobPostingsQuery : IRequest<List<JobPostingDto>>
    {
        public string? Status { get; set; }
        public string? PostingType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}