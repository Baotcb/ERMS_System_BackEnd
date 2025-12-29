using MediatR;
using System;

namespace ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting
{
    public class DeleteJobPostingCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}