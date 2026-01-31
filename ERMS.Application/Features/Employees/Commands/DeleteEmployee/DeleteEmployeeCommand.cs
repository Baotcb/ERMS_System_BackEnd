using MediatR;
using System;

namespace ERMS.Application.Features.Employees.Commands.DeleteEmployee
{
    public sealed class DeleteEmployeeCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public Guid EnterpriseId { get; set; }
    }
}
