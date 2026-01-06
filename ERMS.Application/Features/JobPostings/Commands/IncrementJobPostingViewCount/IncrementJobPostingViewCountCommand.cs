using MediatR;
using System;

namespace ERMS.Application.Features.JobPostings.Commands.IncrementJobPostingViewCount
{
    public class IncrementJobPostingViewCountCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}

