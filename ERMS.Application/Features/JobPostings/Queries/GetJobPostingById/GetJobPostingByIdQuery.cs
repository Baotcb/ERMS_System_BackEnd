using ERMS.Application.Features.JobPostings.DTOs;
using MediatR;
using System;

namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostingById
{
    public class GetJobPostingByIdQuery : IRequest<JobPostingDto>
    {
        public Guid Id { get; set; }
    }
}