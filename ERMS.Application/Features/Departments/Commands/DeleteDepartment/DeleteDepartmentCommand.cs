using MediatR;
using System;

namespace ERMS.Application.Features.Departments.Commands.DeleteDepartment
{
    public sealed class DeleteDepartmentCommand : IRequest<bool>
    {
        public int Id { get; set; }
        public Guid EnterpriseId { get; set; }
    }
}
