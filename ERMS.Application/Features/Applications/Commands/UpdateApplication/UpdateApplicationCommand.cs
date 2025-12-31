using MediatR;
using System;

namespace ERMS.Application.Features.Applications.Commands.UpdateApplication
{
    public class UpdateApplicationCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? CoverLetter { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public double? MatchingScore { get; set; }
        public string? WithdrawReason { get; set; }
    }
}

