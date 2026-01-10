using MediatR;
using System;

namespace ERMS.Application.Features.Applications.Commands.DeleteApplication
{
    public class DeleteApplicationCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}

