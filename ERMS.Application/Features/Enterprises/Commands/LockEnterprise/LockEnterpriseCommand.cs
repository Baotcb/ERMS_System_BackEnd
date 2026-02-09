using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Enterprises.Commands.LockEnterprise
{
    public class LockEnterpriseCommand : IRequest<bool>
    {
        public Guid EnterpriseId { get; set; }
        public bool IsLocked { get; set; } // true = lock, false = unlock

    }
}
