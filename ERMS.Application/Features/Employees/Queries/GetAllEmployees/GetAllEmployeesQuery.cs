using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Employees.Queries.GetAllEmployees
{
    public sealed class GetAllEmployeesQuery : IRequest<GetAllEmployeesResult>
    {

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? DepartmentId { get; set; }
        public string? Status { get; set; }
    }

    public sealed class GetAllEmployeesResult
    {
        public List<EmployeeDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public sealed class EmployeeDto
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
        public List<string> Roles { get; set; } = new();
    }
}
