using MediatR;
using System;

namespace ERMS.Application.Features.JobPostings.Commands.SaveJobPosting
{
    public class SaveJobPostingCommand : IRequest<Guid>
    {
        public Guid JobPostingId { get; set; }
    }
}
