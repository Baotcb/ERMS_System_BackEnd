using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Departments.Queries.GetAllDepartments
{
    public sealed class GetAllDepartmentsQuery : IRequest<GetAllDepartmentsResult>
    {

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed class GetAllDepartmentsResult
    {
        public List<DepartmentDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public sealed class DepartmentDto
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = null!;
        public string? DepartmentCode { get; set; }
        public string? Description { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int? ParentDepartmentId { get; set; }
        public string? ParentDepartmentName { get; set; }
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
