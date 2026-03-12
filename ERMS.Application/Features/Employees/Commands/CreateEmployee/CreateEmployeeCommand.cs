using MediatR;
using System;

namespace ERMS.Application.Features.Employees.Commands.CreateEmployee
{
    public sealed class CreateEmployeeCommand : IRequest<Guid>
    {
        // Account info
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Password { get; set; } = null!;

        // Employee info
        public Guid EnterpriseId { get; set; }
        public int? DepartmentId { get; set; }
        public string Role { get; set; } = null!;
        public string? Position { get; set; }
        public string EmploymentType { get; set; } = "FullTime";
        public DateTime? HireDate { get; set; }
        public Guid? ManagerId { get; set; }
    }
}
