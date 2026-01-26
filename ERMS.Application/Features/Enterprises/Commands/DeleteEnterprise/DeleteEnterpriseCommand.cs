using MediatR;
using System;

namespace ERMS.Application.Features.Enterprises.Commands.DeleteEnterprise
{
    public class DeleteEnterpriseCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
