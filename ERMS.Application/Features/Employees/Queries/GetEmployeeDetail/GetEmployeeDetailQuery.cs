using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Employees.Queries.GetEmployeeDetail
{
    public sealed class GetEmployeeDetailQuery : IRequest<EmployeeDetailDto?>
    {
        public Guid Id { get; set; }
    }

    public sealed class EmployeeDetailDto
    {
        public Guid Id { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Position { get; set; }
        public string EmploymentType { get; set; } = null!;
        public DateTime? HireDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public Guid? ManagerId { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
