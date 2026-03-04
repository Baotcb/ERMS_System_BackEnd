using MediatR;
using System;

namespace ERMS.Application.Features.Departments.Commands.CreateDepartment
{
    public sealed class CreateDepartmentCommand : IRequest<int>
    {
        public Guid EnterpriseId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public string? DepartmentCode { get; set; }
        public string? Description { get; set; }
        public Guid? ManagerId { get; set; }
        public int? ParentDepartmentId { get; set; }
    }
}
