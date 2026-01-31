using MediatR;
using System;

namespace ERMS.Application.Features.Departments.Commands.UpdateDepartment
{
    public sealed class UpdateDepartmentCommand : IRequest<bool>
    {
        public int Id { get; set; }
        public Guid EnterpriseId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public string? DepartmentCode { get; set; }
        public string? Description { get; set; }
        public Guid? ManagerId { get; set; }
        public int? ParentDepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
