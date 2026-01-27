using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Employees.Commands.BulkCreateEmployees
{
    public sealed class BulkCreateEmployeesCommand : IRequest<BulkCreateResult>
    {
        public Guid EnterpriseId { get; set; }
        public List<EmployeeImportItem> Items { get; set; } = new();
    }

    public sealed class EmployeeImportItem
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string DepartmentCode { get; set; } = null!;
        public string? Position { get; set; }
        public string Password { get; set; } = null!;
        public string? Role { get; set; } // Optional: Employee, Trainer, Director, DepartmentHead
    }

    public sealed class BulkCreateResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkCreateError> Errors { get; set; } = new();
    }

    public sealed class BulkCreateError
    {
        public int RowIndex { get; set; }
        public string Email { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
    }
}
