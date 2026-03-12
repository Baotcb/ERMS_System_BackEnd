using MediatR;
using System;

namespace ERMS.Application.Features.Employees.Commands.UpdateEmployee
{
    public sealed class UpdateEmployeeCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public int DepartmentId { get; set; }
        public string? Position { get; set; }
        public string EmploymentType { get; set; } = "FullTime";
        public Guid? ManagerId { get; set; }
        public string Status { get; set; } = "Active";
        public string? Role { get; set; }
    }
}
